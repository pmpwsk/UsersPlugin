let code = document.querySelector("#code");
let continueButton = document.querySelector("#continueButton").firstElementChild;

async function Continue() {
    HideError();
    if (code.value === "")
        ShowError("Enter the current code or a recovery code.");
    else {
        continueButton.innerText = "Loading...";
        switch (await SendRequest(`2fa/try?code=${encodeURIComponent(code.value)}`, "POST")) {
            case "ok": Redirect(); break;
            case "no": ShowError("Invalid code."); break;
            default: ShowError("Connection failed."); break;
        }
        continueButton.innerText = "Continue";
    }
}