
if (document.getElementById('login') != null)
    document.getElementById('login').addEventListener('submit', handleLogin);

async function handleLogin(event) {
    event.preventDefault();
    var username = this.loginUsername.value;
    var password = this.loginPassword.value;
    var passwordless = this.passwordSwitch.checked;
    let returnUrl;
    if (this.returnUrl) {
        returnUrl = this.returnUrl.value;
    }
    else {
        returnUrl = null;
    }

    var formData = new FormData();
    formData.append('Username', username);
    formData.append('Password', password);
    if (returnUrl != null) {
        formData.append('ReturnUrl', returnUrl);
    }
    let loginResult;
    try {
        var res = await fetch('/Account/Login', { //Send request for login
            method: 'POST', 
            body: formData, 
            headers: {
                'Accept': 'application/json'
            }
        });
        if (res == null) {
            handleUnsuccessfullLogin(null);
            return;
        }

        loginResult = await res.json();
    } catch (e) {
        // showErrorAlert("Request to server failed", e);
        handleUnsuccessfullLogin("Request to server failed");
        return;
    }

    if (!loginResult.success) {
        handleUnsuccessfullLogin(loginResult.message);
        return;
    }

    if (!loginResult.twoFactor) {
        window.location.href = '/Fido/Devices';
        return;
    }

    document.getElementById("loginModalTitle").innerHTML = "Login with authenticator";
    document.getElementById("loginModalBody").innerHTML = "Please use your authenticator to finish login";

    let makeAssertionOptions = loginResult.makeAssertionOptions;
    // show options error to user
    if (makeAssertionOptions.status !== "ok") {
        handleUnsuccessfullLogin(makeAssertionOptions.errorMessage);
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
        handleUnsuccessfullLogin(err.message ? err.message : err);
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
        handleUnsuccessfullLogin("Request to server failed", e);
        throw e;

    }

    console.log("Assertion Object", response);

    // show error
    if (response.success !== true) {
        console.log("Error doing assertion");
        console.log(response.errorMessage);
        handleUnsuccessfullLogin(response.errorMessage);
        return;
    }

    // redirect to dashboard to show keys
    window.location.href = "/Account/Fido2LoginSuccess";
}

function handleUnsuccessfullLogin(message) {
    if (!message) {
        document.getElementById("loginModalTitle").innerHTML = "Error";
        document.getElementById("loginModalBody").innerHTML = "Login error";
    }
    else {
        document.getElementById("loginModalTitle").innerHTML = "Error";
        document.getElementById("loginModalBody").innerHTML = message;
    }
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
