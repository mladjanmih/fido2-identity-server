if (document.getElementById('registerCertificate') != null)
    document.getElementById('registerCertificate').addEventListener('submit', handleSmartCardRegister);

async function handleSmartCardRegister(event) {
    event.preventDefault();
    let regCertOptions;
    try {
        let optRes = await fetch("/Certificate/GetRegisterCertificateOptions", {
            method: 'GET'
        });
        regCertOptions = await optRes.json();
    }
    catch (err) {
        smartCardRegistrationFailed();
        document.getElementById("regCertModalTitle").innerHTML = "Certificate Registration Error";
        document.getElementById("regCertModalBody").innerHTML = "Error in communication with server.";
        return;
    }
    document.getElementById("regCertModalTitle").innerHTML = "Certificate Registration";
    document.getElementById("regCertModalBody").innerHTML = "Please run your desktop application in order to register smart card. When you run application, click on Register.";
    document.getElementById("regCertBttn").hidden = false;
    var payload = regCertOptions.challenge;
    document.getElementById("payload").value = payload; 
    if (document.getElementById('regCert') != null)
        document.getElementById('regCert').addEventListener('submit', registerSmartCard);

}
async function registerSmartCard(event) {
    event.preventDefault();
    var payload = document.getElementById("payload").value;
    var formData = new FormData();
    formData.append("payload", payload);
    let res;
    let jsonRes;
    try {
        res = await fetch("https://localhost:5001/signature", {
            method: 'POST', // or 'PUT'
            body: formData
        })
        .then(response => response.json())
            .then(jsondata => {
                console.log(jsondata);
                jsonRes = jsondata;
        });
    }
    catch (err) {
        console.log(err);
        smartCardRegistrationFailed();
        return;
    }

    try {

        let verifyResult = await fetch("/Certificate/RegisterCertificateCallback", {
            method: 'POST', // or 'PUT'
            body: JSON.stringify(jsonRes), // data can be `string` or {object}
            headers: {
                'Content-Type': 'application/json'
            }
        });
        verify = await verifyResult.json();
        if (verify.success == true) {
            window.location.href = "/Certificate/RegistrationFinished?success=true";
        }
        else {
            smartCardRegistrationFailed();
            return;
        }
    }
    catch (err) {
        console.log(err);
        smartCardAuthorizationFailed();
        return;
    }
}

function smartCardRegistrationFailed() {
    document.getElementById("regCertModalTitle").innerHTML = "Certificate registration";
    document.getElementById("regCertModalBody").innerHTML = "Authorization failed. Please run your desktop application in order to register smart card. When you run application, click on Register.";
}