if (document.getElementById('registerDevice') != null)
    document.getElementById('registerDevice').addEventListener('submit', registerDevice);

async function registerDevice(event) {
    event.preventDefault();
    let makeCredentialOptions;
    var formData = new FormData();
   
    try {
        var res = await fetch('/Fido/FidoRegister', {
            method: 'POST', // or 'PUT'
            body: formData, // data can be `string` or {object}!
            headers: {
                'Accept': 'application/json'
            }
        });
        if (res == null) {
            handleUnsuccessfullDeviceRegister(null);
            return;
        }
        makeCredentialOptions = await res.json();
    } catch (e) {
        // showErrorAlert("Request to server failed", e);
        handleUnsuccessfullDeviceRegister("Request to server failed");
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
        }).then((newCredential) => {

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
                        handleUnsuccessfullDeviceRegister("Could not create credentials in browser.");
                }

            });
        },
            (err) => { handleUnsuccessfullDeviceRegister("Could not create credentials in browser."); });
    } catch (e) {
        var msg = "Could not create credentials in browser. Probably because the username is already registered with your authenticator. Please change username or authenticator."
        console.error(msg, e);
        handleUnsuccessfullDeviceRegister(msg);
        return;
        //window.location.href = "/Fido/RegisterFailed";
    }
}

function handleUnsuccessfullDeviceRegister(message) {
    if (!message) {
        document.getElementById("loginModalTitle").innerHTML = "Error";
        document.getElementById("loginModalBody").innerHTML = "Login error";
    }
    else {
        document.getElementById("loginModalTitle").innerHTML = "Error";
        document.getElementById("loginModalBody").innerHTML = message;
    }
}