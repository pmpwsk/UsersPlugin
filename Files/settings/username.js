let username = document.querySelector("#username");
let password = document.querySelector("#password");
let code = document.querySelector("#code");
let continueButton = document.querySelector("#continueButton").firstElementChild;

async function Continue() {
    HideError();
    if (username.value === "") {
        ShowError("Enter a username.");
    } else if (password.value === "") {
        ShowError("Enter your password.");
    } else if (code.value === "") {
        ShowError("Enter the current code or a recovery code.");
    } else {
        continueButton.innerText = "Loading...";
        switch (await SendRequest(`username/set?username=${encodeURIComponent(username.value)}&code=${encodeURIComponent(code.value)}&password=${encodeURIComponent(password.value)}`, "POST")) {
            case "ok": window.location.assign("../settings"); break;
            case "no": ShowError("Invalid password or 2FA code."); break;
            case "bad": ShowError("Usernames must be at least 3 characters long and only contain lowercase letters, digits, dashes, dots and underscores. The first and last characters can only be letters or digits."); break;
            case "exists": ShowError("This username is already being used by another account."); break;
            case "same": ShowError("The provided username is the same as the old one."); break;
            default: ShowError("Connection failed."); break;
        }
        continueButton.innerText = "Change";
    }
}