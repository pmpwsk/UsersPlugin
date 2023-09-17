let code = document.querySelector("#code");

async function Continue() {
    if (code.value === "") {
        ShowError("Enter the current code or a recovery code.");
    } else {
        let response = await fetch("/api[PATH_PREFIX]/2fa?code=" + encodeURIComponent(code.value));
        if (response.status === 200) {
            let text = await response.text();
            if (text === "no") {
                ShowError("Invalid code.");
            } else if (text === "ok") {
                Redirect();
            } else {
                ShowError("Connection failed.");
            }
        } else {
            ShowError("Connection failed.");
        }
    }
}