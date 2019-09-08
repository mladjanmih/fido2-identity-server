if (document.getElementById('digital-signature') != null) 
    document.getElementById('digital-signature').addEventListener('submit', handleDSigAuthenticatorSelectSubmit);
//if (document.getElementById('windows-hello-dsig') != null)
//    document.getElementById('windows-hello-dsig').addEventListener('submit', handleWindowsHelloDSigSubmit);

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
    formData.append("authenticator", authenticator);

    await handleDSigSubmit(formData, paymentId);
}

//async function handleWindowsHelloDSigSubmit(event) {
//    event.preventDefault();

//    let challenge = this.windowsHelloChallenge.value;

//    // prepare form post data
//    var formData = new FormData();
//    formData.append('dsigType', "windows-hello");

//    await handleDSigSubmit(formData, challenge);
//}

async function handleDSigSubmit(formData, paymentId) {
    // send to server for registering
    let makeAssertionOptions;
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
        makeAssertionOptions = await res.json();
    } catch (e) {
       // showErrorAlert("Request to server failed", e);
        window.location.href = "/Fido/DigitalSigningFailed";
        return;
    }

    console.log("Assertion Options Object", makeAssertionOptions);

    // show options error to user
    if (makeAssertionOptions.status !== "ok") {
        console.log("Error creating assertion options");
        console.log(makeAssertionOptions.errorMessage);
       // showErrorAlert(makeAssertionOptions.errorMessage);
        window.location.href = "/Fido/DigitalSigningFailed";
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
        window.location.href = "/Fido/DigitalSigningFailed";
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
        window.location.href = "/Fido/DigitalSigningFailed";
        throw e;
       
    }

    console.log("Assertion Object", response);

    // show error
    if (response.success !== true) {
        console.log("Error doing assertion");
        console.log(response.errorMessage);
 //       showErrorAlert(response.errorMessage);
        window.location.href = "/Fido/DigitalSigningFailed";
        return;
    }

    // redirect to dashboard to show keys
    window.location.href = "/Fido/DigitalSigningSuccess";
}
