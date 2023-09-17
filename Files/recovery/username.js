let email = document.querySelector("#email");

async function Continue(url) {
    if (email.value === "") {
        ShowError("Enter your email address.");
    } else {
        let response = await fetch("/api[PATH_PREFIX]/recovery/username?email=" + encodeURIComponent(email.value));
        if (response.status === 200) {
            let text = await response.text();
            switch (text) {
                case "ok": window.location.assign(url); break;
                case "no": ShowError("Invalid email address."); break;
                default: ShowError("Connection failed.");
            }
        } else {
            ShowError("Connection failed.");
        }
    }
}