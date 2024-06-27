let code = document.querySelector("#code");
let continueButton = document.querySelector("#continueButton").firstElementChild;
let resendButton = document.querySelector("#resendButton");

async function Continue() {
    HideError();
    if (code.value === "") {
        ShowError("Enter a code.");
    } else {
        continueButton.innerText = "Loading...";
        switch (await SendRequest(`email/verify?code=${encodeURIComponent(code.value)}`, "POST")) {
            case "ok": window.location.assign("../settings"); break;
            case "no": ShowError("Invalid code."); break;
            case "bad": ShowError("Invalid email address."); break;
            case "exists": ShowError("This email address is already being used by another account."); break;
            case "same": ShowError("The provided email address is the same as the old one."); break;
            default: ShowError("Connection failed."); break;
        }
        continueButton.innerText = "Change";
    }
}

async function Resend() {
    HideError();
    resendButton.innerText = "Sending...";
    if ((await fetch("email/resend", {method:"POST"})).status === 200) {
        resendButton.innerText = "Sent!";
    } else {
        resendButton.innerText = "Send again";
        ShowError("Connection failed.");
    }
}

async function Cancel() {
    HideError();
    if ((await fetch("email/cancel", {method:"POST"})).status === 200)
        window.location.reload();
    else ShowError("Connection failed.");
}