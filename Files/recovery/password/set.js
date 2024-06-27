let password1 = document.querySelector("#password1");
let password2 = document.querySelector("#password2");

async function Continue(token) {
    HideError();
    if (password1.value === "") {
        ShowError("Enter a password.");
    } else if (password2.value === "") {
        ShowError("Confirm the password.");
    } else if (password1.value != password2.value) {
        ShowError("The passwords do not match.");
    } else
        switch (await SendRequest(`password/set?password=${encodeURIComponent(password1.value)}&token=${encodeURIComponent(GetQuery("token"))}`, "POST")) {
            case "ok": window.location.assign(`../login${RedirectQuery()}`); break;
            case "bad": ShowError("Passwords must be at least 8 characters long and contain at least one uppercase letter, lowercase letter, digit and special character."); break;
            case "same": ShowError("The provided password is the same as the old one."); break;
            default: ShowError("Connection failed."); break;
        }
}

function GetQuery(q) {
    try {
        let query = new URLSearchParams(window.location.search);
        if (query.has(q)) {
            return query.get(q);
        } else {
            return "null";
        }
    } catch {
        return "null";
    }
}