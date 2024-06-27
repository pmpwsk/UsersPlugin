let username = document.querySelector("#username");
let password = document.querySelector("#password");
let continueButton = document.querySelector("#continueButton").firstElementChild;

async function Continue() {
    HideError();
    if (username.value === "")
        ShowError("Enter your username.");
    else if (password.value === "")
        ShowError("Enter your password.");
    else {
        continueButton.innerText = "Loading...";
        switch (await SendRequest(`login/try?username=${encodeURIComponent(username.value)}&password=${encodeURIComponent(password.value)}`, "POST")) {
            case "ok": Redirect(); break;
            case "2fa": window.location.assign(`2fa${RedirectQuery()}`); break;
            case "verify": window.location.assign(`verify${RedirectQuery()}`); break;
            case "no": ShowError("Invalid username or password."); break;
            default: ShowError("Connection failed."); break;
        }
        continueButton.innerText = "Continue";
    }
}