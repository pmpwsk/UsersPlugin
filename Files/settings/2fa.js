let code = document.querySelector("#code");
let password = document.querySelector("#password");

async function Continue(method) {
    if (code.value === "") {
        ShowError("Enter the current code or a recovery code.");
    } else if (password.value === "") {
        ShowError("Enter your password.");
    } else {
        let response = await fetch("/api[PATH_PREFIX]/settings/2fa?method=" + method + "&code=" + encodeURIComponent(code.value) + "&password=" + encodeURIComponent(password.value));
        if (response.status === 200) {
            let text = await response.text();
            if (text === "no") {
                ShowError("Invalid code or password.");
            } else if (text === "ok") {
                window.location.assign("[PATH_PREFIX]/settings");
            } else {
                ShowError("Connection failed.");
            }
        } else {
            ShowError("Connection failed.");
        }
    }
}