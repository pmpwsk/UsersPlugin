let password1 = document.querySelector("#password1");
let password2 = document.querySelector("#password2");

async function Continue(url, token) {
    HideError();
    if (password1.value === "") {
        ShowError("Enter a password.");
    } else if (password2.value === "") {
        ShowError("Confirm the password.");
    } else if (password1.value != password2.value) {
        ShowError("The passwords do not match.");
    } else {
        let response = await fetch("/api[PATH_PREFIX]/recovery/password?password=" + encodeURIComponent(password1.value) + "&token=" + token);
        if (response.status === 200) {
            let text = await response.text();
            switch (text) {
                case "ok": window.location.assign(url); break;
                case "bad": ShowError("Passwords must be at least 8 characters long and contain at least one uppercase letter, lowercase letter, digit and special character."); break;
                case "same": ShowError("The provided password is the same as the old one."); break;
                default: ShowError("Connection failed.");
            }
        } else {
            ShowError("Connection failed.");
        }
    }
}