let password = document.querySelector("#password");
let code = document.querySelector("#code");
let continueButton = document.querySelector("#continueButton").firstElementChild;

async function Continue() {
    HideError();
    if (password.value === "") {
        ShowError("Enter your password.");
    } else if (code.value === "") {
        ShowError("Enter the current code or a recovery code.");
    } else {
        continueButton.innerText = "Loading...";
        switch (await SendRequest(`delete/try?code=${encodeURIComponent(code.value)}&password=${encodeURIComponent(password.value)}`, "POST")) {
            case "ok": window.location.assign("/"); break;
            case "no": ShowError("Invalid password or 2FA code."); break;
            default: ShowError("Connection failed."); break;
        }
        continueButton.innerText = "Delete account :(";
    }
}