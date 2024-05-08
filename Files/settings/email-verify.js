let code = document.querySelector("#code");
let continueButton = document.querySelector("#continueButton").firstElementChild;

async function Continue() {
    HideError();
    if (code.value === "") {
        ShowError("Enter a code.");
    } else {
        continueButton.innerText = "Loading...";
        let response = await fetch("/api[PATH_PREFIX]/settings/email?code=" + encodeURIComponent(code.value));
        if (response.status === 200) {
            let text = await response.text();
            switch (text) {
                case "ok": window.location.assign("[PATH_PREFIX]/settings"); break;
                case "no": ShowError("Invalid code."); break;
                case "bad": ShowError("Invalid email address."); break;
                case "exists": ShowError("This email address is already being used by another account."); break;
                case "same": ShowError("The provided email address is the same as the old one."); break;
                default: ShowError("Connection failed.");
            }
        } else {
            ShowError("Connection failed.");
        }
        continueButton.innerText = "Change";
    }
}

async function Resend() {
    HideError();
    let response = await fetch("/api[PATH_PREFIX]/settings/email?resend=please");
    if (response.status != 200) {
        ShowError("Connection failed.");
    }
}