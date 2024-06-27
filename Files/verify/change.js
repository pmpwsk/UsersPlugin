let code = document.querySelector("#email");

async function Continue() {
    HideError();
    if (code.value === "")
        ShowError("Enter your email address.");
    else
        switch (await SendRequest(`set-email?email=${encodeURIComponent(code.value)}`, "POST")) {
            case "ok": Redirect(); break;
            case "bad": ShowError("Invalid email address."); break;
            case "exists": ShowError("This email address is already being used by another account."); break;
            case "same": ShowError("The provided email address is the same as the old one."); break;
            default: ShowError("Connection failed."); break;
        }
}