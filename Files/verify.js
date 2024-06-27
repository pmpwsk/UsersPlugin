let code = document.querySelector("#code");

async function Continue() {
    HideError();
    if (code.value === "")
        ShowError("Enter a code.");
    else 
        switch (await SendRequest(`verify/try?code=${encodeURIComponent(code.value)}`, "POST")) {
            case "ok": Redirect(); break;
            case "no": ShowError("Invalid code."); break;
            default: ShowError("Connection failed."); break;
        }
}

async function Resend() {
    HideError();
    if ((await fetch("verify/resend", {method:"POST"})).status !== 200)
        ShowError("Connection failed.");
}