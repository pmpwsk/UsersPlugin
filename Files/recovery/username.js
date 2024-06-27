let email = document.querySelector("#email");

async function Continue() {
    HideError();
    if (email.value === "") {
        ShowError("Enter your email address.");
    } else
        switch (await SendRequest(`username/request?email=${encodeURIComponent(email.value)}`, "POST")) {
            case "ok": window.location.assign(`../login${RedirectQuery()}`); break;
            case "no": ShowError("Invalid email address."); break;
            default: ShowError("Connection failed."); break;
        }
}