if (document.getElementById('yubikey') != null) 
    document.getElementById('yubikey').addEventListener('submit', handleYubikeySignInSubmit);
if (document.getElementById('windows-hello') != null) 
    document.getElementById('windows-hello').addEventListener('submit', handleWindowsHelloSignInSubmit);


async function handleYubikeySignInSubmit(event) {
    event.preventDefault();

    // passwordfield is omitted in demo
    // let password = this.password.value;


    // prepare form post data
    var formData = new FormData();
    formData.append('loginType', "yubikey");

    await handleSignInSubmit(formData);
    // not done in demo
    // todo: validate username + password with server (has nothing to do with FIDO2/WebAuthn)
}

async function handleWindowsHelloSignInSubmit(event) {
    event.preventDefault();

 //   let username = this.username.value;

    // passwordfield is omitted in demo
    // let password = this.password.value;


    // prepare form post data
    var formData = new FormData();
    formData.append('loginType', "windows-hello");

    await handleSignInSubmit(formData);
    // not done in demo
    // todo: validate username + password with server (has nothing to do with FIDO2/WebAuthn)
}

async function handleSignInSubmit(formData) {
    // send to server for registering
    let makeAssertionOptions;
    try {
        var res = await fetch('/Account/Fido2Login', {
            method: 'POST', // or 'PUT'
            body: formData, // data can be `string` or {object}!
            headers: {
                'Accept': 'application/json'
            }
        });
        if (res == null) {
            window.location.href = "/Account/Fido2LoginFailed";
        }
        makeAssertionOptions = await res.json();
    } catch (e) {
       // showErrorAlert("Request to server failed", e);
        window.location.href = "/Account/Fido2LoginFailed";
        return;
    }

    console.log("Assertion Options Object", makeAssertionOptions);

    // show options error to user
    if (makeAssertionOptions.status !== "ok") {
        console.log("Error creating assertion options");
        console.log(makeAssertionOptions.errorMessage);
       // showErrorAlert(makeAssertionOptions.errorMessage);
        window.location.href = "/Account/Fido2LoginFailed";
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
      //  showErrorAlert(err.message ? err.message : err);
        window.location.href = "/Account/Fido2LoginFailed";
    }

    //try {
    //    await verifyAssertionWithServer(credential);
    //} catch (e) {
    // //   showErrorAlert("Could not verify assertion", e);
    //    window.location.href = "/Account/Fido2LoginFailed";
    //}

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
        let res = await fetch("/Account/Fido2LoginCallback", {
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
        window.location.href = "/Account/Fido2LoginFailed";
        throw e;
       
    }

    console.log("Assertion Object", response);

    // show error
    if (response.success !== true) {
        console.log("Error doing assertion");
        console.log(response.errorMessage);
 //       showErrorAlert(response.errorMessage);
        window.location.href = "/Account/Fido2LoginFailed";
        return;
    }

    // redirect to dashboard to show keys
    window.location.href = "/Account/Fido2LoginSuccess";
}

//makeAssertion = function (makeAssertionOptions) {
//    console.log("Assertion Options Object", makeAssertionOptions);

//    // show options error to user
//    if (makeAssertionOptions.status !== "ok") {
//        console.log("Error creating assertion options");
//        console.log(makeAssertionOptions.errorMessage);
//        showErrorAlert(makeAssertionOptions.errorMessage);
//        return;
//    }

//    // todo: switch this to coercebase64
//    const challenge = makeAssertionOptions.challenge.replace(/-/g, "+").replace(/_/g, "/");
//    makeAssertionOptions.challenge = Uint8Array.from(atob(challenge), c => c.charCodeAt(0));

//    // fix escaping. Change this to coerce
//    makeAssertionOptions.allowCredentials.forEach(function (listItem) {
//        var fixedId = listItem.id.replace(/\_/g, "/").replace(/\-/g, "+");
//        listItem.id = Uint8Array.from(atob(fixedId), c => c.charCodeAt(0));
//    });

//    console.log("Assertion options", makeAssertionOptions);
//    try {
//        navigator.credentials.get({ publicKey: makeAssertionOptions })
//            .then(
//                (credential) => {
//                    let authData = new Uint8Array(assertedCredential.response.authenticatorData);
//                    let clientDataJSON = new Uint8Array(assertedCredential.response.clientDataJSON);
//                    let rawId = new Uint8Array(assertedCredential.rawId);
//                    let sig = new Uint8Array(assertedCredential.response.signature);
//                    const data = {
//                        id: assertedCredential.id,
//                        rawId: coerceToBase64Url(rawId),
//                        type: assertedCredential.type,
//                        extensions: assertedCredential.getClientExtensionResults(),
//                        response: {
//                            authenticatorData: coerceToBase64Url(authData),
//                            clientDataJson: coerceToBase64Url(clientDataJSON),
//                            signature: coerceToBase64Url(sig)
//                        }
//                    };

//                    $.ajax({
//                        url: '/Account/Fido2LoginCallback',
//                        type: 'POST',
//                        contentType: 'application/json',
//                        data: JSON.stringify(data),
//                        success: function (success) {
//                            if (success)
//                                window.location.href = "/Account/Fido2LoginSuccess";
//                            else
//                                window.location.href = "/Account/Fido2LoginFailed";
//                        }
//                    });
//                }, (err) => {
//                    window.location.href = "/Account/LoginFailed";
//                });
//    }
//    catch (err) {
//        showErrorAlert(err.message ? err.message : err);
//        window.location.href = "/Account/LoginFailed";
//    }
//}