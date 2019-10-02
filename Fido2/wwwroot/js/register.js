if (document.getElementById('register') != null)
    document.getElementById('register').addEventListener('submit', registerWithPassword);


async function registerWithPassword() {
    let makeCredentialOptions;
    var formData = new FormData();
    let username = this.username.value;
    let password = this.password.value;
    let confirmPassword = this.confirmPassword.value;
    let email = this.email.value;
    formData.append('Username', username);
    formData.append('Password', password);
    formData.append('ConfirmPassword', confirmPassword);
    formData.append('Email', email);

    try {
        var res = await fetch('/Account/Register', {
            method: 'POST', // or 'PUT'
            body: formData, // data can be `string` or {object}!
            headers: {
                'Accept': 'application/json'
            }
        });
        if (res == null) {
            window.location.href = "/Account/Fido2LoginFailed";
        }
        makeCredentialOptions = await res.json();
    } catch (e) {
        // showErrorAlert("Request to server failed", e);
        window.location.href = "/Account/Fido2LoginFailed";
        return;
    }
  //  var makeCredentialOptions = @Html.Raw(Json.Serialize(Model.CredentialCreateOptions));
    console.log("Credential Options Object", makeCredentialOptions);

    makeCredentialOptions.challenge = coerceToArrayBuffer(makeCredentialOptions.challenge);
    makeCredentialOptions.user.id = coerceToArrayBuffer(makeCredentialOptions.user.id);
    makeCredentialOptions.excludeCredentials = makeCredentialOptions.excludeCredentials.map((c) => {
        c.id = coerceToArrayBuffer(c.id);
        return c;
    });

    if (makeCredentialOptions.authenticatorSelection.authenticatorAttachment === null)
        makeCredentialOptions.authenticatorSelection.authenticatorAttachment = undefined;

    console.log("Credential Options Formatted", makeCredentialOptions);

    let newCredential;
    try {
        newCredential = navigator.credentials.create({
            publicKey: makeCredentialOptions
        });
    }
    catch (err) {
        handleUnsuccessfullRegister(err.message ? err.message : err);
        return;
    }
    .then((newCredential) => {

            console.log("PublicKeyCredential Created", newCredential);

            let attestationObject = new Uint8Array(newCredential.response.attestationObject);
            let clientDataJSON = new Uint8Array(newCredential.response.clientDataJSON);
            let rawId = new Uint8Array(newCredential.rawId);

            const data = {
                id: newCredential.id,
                rawId: coerceToBase64Url(rawId),
                type: newCredential.type,
                extensions: newCredential.getClientExtensionResults(),
                response: {
                    AttestationObject: coerceToBase64Url(attestationObject),
                    clientDataJson: coerceToBase64Url(clientDataJSON)
                }
            };

            $.ajax({
                url: '/Fido/RegisterCallback',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(data),
                success: function (success) {
                    if (success)
                        window.location.href = "/Fido/RegisterSuccess";
                    else
                        window.location.href = "/Fido/RegisterFailed";
                }

            });
        },
            (err) => { window.location.href = "/Fido/RegisterFailed"; });
    } catch (e) {
        var msg = "Could not create credentials in browser. Probably because the username is already registered with your authenticator. Please change username or authenticator."
        console.error(msg, e);
        showErrorAlert(msg, e);
        window.location.href = "/Fido/RegisterFailed";
    }
}

