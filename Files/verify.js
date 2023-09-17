let code = document.querySelector("#code");

async function Continue() {
    if (code.value === "") {
        ShowError("Enter a code.");
    } else {
        let response = await fetch("/api[PATH_PREFIX]/verify?code=" + encodeURIComponent(code.value));
        if (response.status === 200) {
            let text = await response.text();
            if (text === "ok") {
                Redirect();
            } else {
                ShowError(text);
            }
        } else {
            ShowError("Connection failed.");
        }
    }
}

async function Resend() {
    let response = await fetch("/api[PATH_PREFIX]/verify?resend=please");
    if (response.status != 200) {
        ShowError("Connection failed.");
    }
}