let password = document.querySelector("#password");
let code = document.querySelector("#code");

async function Continue() {
    if (password.value === "") {
        ShowError("Enter your password.");
    } else if (code.value === "") {
        ShowError("Enter the current code or a recovery code.");
    } else {
        let response = await fetch("/api[PATH_PREFIX]/settings/delete?code=" + encodeURIComponent(code.value) + "&password=" + encodeURIComponent(password.value));
        if (response.status === 200) {
            let text = await response.text();
            switch (text) {
                case "ok": window.location.assign("/"); break;
                case "no": ShowError("Invalid password or 2FA code."); break;
                default: ShowError("Connection failed.");
            }
        } else {
            ShowError("Connection failed.");
        }
    }
}