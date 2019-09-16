if (document.getElementById('digital-signature') != null) 
    document.getElementById('digital-signature').addEventListener('submit', handleDSigAuthenticatorSelectSubmit);

async function handleDSigAuthenticatorSelectSubmit(event) {
    event.preventDefault();

    let paymentId = document.getElementById('paymentId').value;

    var authenticators = document.getElementsByName('authenticator');
    var authenticator;
    for (var i = 0; i < authenticators.length; i++) {
        if (authenticators[i].checked) {
            authenticator = authenticators[i].value;
        }
    }

    // prepare form post data
    var formData = new FormData();
    formData.append("authType", authenticator);

    await handleDSigSubmit(formData, paymentId);
}

async function handleDSigSubmit(formData, paymentId) {
    // send to server for registering
    let options;
    try {
        var res = await fetch('/Fido/AssertDigitalSignature', {
            method: 'POST', // or 'PUT'
            body: formData, // data can be `string` or {object}!
            headers: {
                'Accept': 'application/json',
                'PaymentId': paymentId
            }
        });
        if (res == null) {
            window.location.href = "/Fido/DigitalSigningFailed";
        }
        options = await res.json();
    } catch (e) {
       // showErrorAlert("Request to server failed", e);
        window.location.href = "/Fido/DigitalSigningFailed";
        return;
    }

    let makeAssertionOptions, smartCardOptions;
    let fido2, smartCard;

    if (options.type) {
        if (options.type == "fido2") {
            fido2 = true;
            smartCard = false;
            makeAssertionOptions = options.makeAssertionOptions;
        }
        else if (options.type == "smart_card") {
            fido2 = false;
            smartCard = true;
            smartCardOptions = options.smartCardOptions;
        }
    }
    else {
        handleUnsuccessfulDsig(options.message)
    }

    if (fido2) {
        await fido2Dsig(makeAssertionOptions);
    }
    else if (smartCard) {
        await smartCardDsig(smartCardOptions);
    }

    else {
        handleUnsuccessfulDsig(options.message)
    }
}


async function fido2Dsig(makeAssertionOptions) {
    console.log("Assertion Options Object", makeAssertionOptions);
    document.getElementById("dsigModalTitle").innerHTML = "Digital signature";
    document.getElementById("dsigModalBody").innerHTML = "Please use your FIDO2 authenticator to digitaly sign the payment.";

    // show options error to user
    if (makeAssertionOptions.status !== "ok") {
        handleUnsuccessfulDsig("Error creating assertion data.");
        return;
    }

    // todo: switch this to coercebase64
    const challenge = makeAssertionOptions.challenge.replace(/-/g, "+").replace(/_/g, "/");
    makeAssertionOptions.challenge = Uint8Array.from(atob(challenge), c => c.charCodeAt(0));

    // fix escaping. Change this to coerce
    makeAssertionOptions.allowCredentials.forEach(function (listItem) {
        var fixedId = listItem.id.replace(/\_/g, "/").replace(/\-/g, "+");
        listItem.id = Uint8Array.from(atob(fixedId), c => c.charCodeAt(0));
    });

    console.log("Assertion options", makeAssertionOptions);

    // ask browser for credentials (browser will ask connected authenticators)
    let credential;
    try {
        credential = await navigator.credentials.get({ publicKey: makeAssertionOptions })
    } catch (err) {
        handleUnsuccessfulDsig("Error in communication with authenticator!");
        return;
    }

    let authData = new Uint8Array(credential.response.authenticatorData);
    let clientDataJSON = new Uint8Array(credential.response.clientDataJSON);
    let rawId = new Uint8Array(credential.rawId);
    let sig = new Uint8Array(credential.response.signature);
    const data = {
        id: credential.id,
        rawId: coerceToBase64Url(rawId),
        type: credential.type,
        extensions: credential.getClientExtensionResults(),
        response: {
            authenticatorData: coerceToBase64Url(authData),
            clientDataJson: coerceToBase64Url(clientDataJSON),
            signature: coerceToBase64Url(sig)
        }
    };

    let response;
    try {
        let res = await fetch("/Fido/AssertDigitalSignatureResult", {
            method: 'POST', // or 'PUT'
            body: JSON.stringify(data), // data can be `string` or {object}!
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });

        response = await res.json();
    } catch (e) {
        //   showErrorAlert("Request to server failed", e);
        handleUnsuccessfulDsig("Signature data validation failed.");
        throw e;

    }

    console.log("Assertion Object", response);

    // show error
    if (response.success !== true) {
        console.log("Error doing assertion");
        console.log(response.errorMessage);
        handleUnsuccessfulDsig(response.errorMessage);
        return;
    }

    // redirect to dashboard to show keys
    window.location.href = "/Fido/DigitalSigningSuccess";
}

async function smartCardDsig(smartCardOptions) {
    document.getElementById("dsigModalTitle").innerHTML = "Digital signature";
    document.getElementById("dsigModalBody").innerHTML = "Please run your desktop application to digitally sign the payment with smart card. When you run application, click on Authorize.";
    document.getElementById("smart_card_signature_button").hidden = false;
    document.getElementById("payload").value = smartCardOptions.payload; 

    if (document.getElementById('smart_card_signature') != null)
        document.getElementById('smart_card_signature').addEventListener('submit', handleSmartCardDsig);

    
}

async function handleSmartCardDsig(event) {
    event.preventDefault();
   var payload =  document.getElementById("payload").value;
    var formData = new FormData();
    formData.append("payload", payload);
    let res;
    let jsonRes;
    try {
        res = await fetch("https://localhost:5001/signature", {
            method: 'POST', // or 'PUT'
            body: formData // data can be `string` or {object}
        }).then(response => response.json())
            .then(jsondata => {
                console.log(jsondata);
                jsonRes = jsondata;
            });
    }
    catch (err) {
        console.log(err);
        smartCardAuthorizationFailed();
        return;
    }

    try {

        let verifyResult = await fetch("/Fido/SmartCardDigitalSignatureCallback", {
            method: 'POST', // or 'PUT'
            body: JSON.stringify(jsonRes), // data can be `string` or {object}
            headers: {
                'Content-Type': 'application/json'
            }
        });
        verify = await verifyResult.json();
        if (verify.success == true) {
            window.location.href = "/Fido/DigitalSigningSuccess";
        }
        else {
            smartCardAuthorizationFailed();
            return;
        }
    }
    catch (err) {
        console.log(err);
        smartCardAuthorizationFailed();
       return;
    }
}

function handleUnsuccessfulDsig(message) {
    if (!message) {
        document.getElementById("dsigModalTitle").innerHTML = "Error";
        document.getElementById("dsigModalBody").innerHTML = "Digital signature error";
    }
    else {
        document.getElementById("dsigModalTitle").innerHTML = "Error";
        document.getElementById("dsigModalBody").innerHTML = message;
    }
    document.getElementById("smart_card_signature_button").hidden = true;
}

function smartCardAuthorizationFailed() {
    document.getElementById("dsigModalTitle").innerHTML = "Payment authorization";
    document.getElementById("dsigModalBody").innerHTML = "Authorization failed. Please run your desktop application to digitally sign the payment with smart card. When you run application, click on Authorize.";
}