let code = document.querySelector("#code");
let password = document.querySelector("#password");
let continueButton = document.querySelector("#continueButton").firstElementChild;

async function Continue(method) {
    HideError();
    if (code.value === "") {
        ShowError("Enter the current code or a recovery code.");
    } else if (password.value === "") {
        ShowError("Enter your password.");
    } else {
        continueButton.innerText = "Loading...";
        switch (await SendRequest(`2fa/change?method=${method}&code=${encodeURIComponent(code.value)}&password=${encodeURIComponent(password.value)}`, "POST")) {
            case "ok": window.location.assign("../settings"); break;
            case "no": ShowError("Invalid code or password."); break;
            default: ShowError("Connection failed."); break;
        }
        continueButton.innerText = method === "enable" ? "Enable" : "Disable";
    }
}