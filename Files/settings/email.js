let email = document.querySelector("#email");
let password = document.querySelector("#password");
let code = document.querySelector("#code");
let continueButton = document.querySelector("#continueButton").firstElementChild;

async function Continue() {
    HideError();
    if (email.value === "") {
        ShowError("Enter the email address.");
    } else if (password.value === "") {
        ShowError("Enter your password.");
    } else if (code.value === "") {
        ShowError("Enter the current code or a recovery code.");
    } else {
        continueButton.innerText = "Loading...";
        switch (await SendRequest(`email/set?email=${encodeURIComponent(email.value)}&code=${encodeURIComponent(code.value)}&password=${encodeURIComponent(password.value)}`, "POST")) {
            case "ok": window.location.reload(); break;
            case "no": ShowError("Invalid password or 2FA code."); break;
            case "bad": ShowError("Invalid email address."); break;
            case "exists": ShowError("This email address is already being used by another account."); break;
            case "same": ShowError("The provided email address is the same as the old one."); break;
            default: ShowError("Connection failed."); break;
        }
        continueButton.innerText = "Change";
    }
}