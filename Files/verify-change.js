let code = document.querySelector("#email");

async function Continue() {
    if (code.value === "") {
        ShowError("Enter your email address.");
    } else {
        let response = await fetch("/api[PATH_PREFIX]/verify-change?email=" + encodeURIComponent(code.value));
        if (response.status === 200) {
            let text = await response.text();
            switch (text) {
                case "ok": Redirect(); break;
                case "bad": ShowError("Invalid email address."); break;
                case "exists": ShowError("This email address is already being used by another account."); break;
                case "same": ShowError("The provided email address is the same as the old one."); break;
                default: ShowError("Connection failed.");
            }
        } else {
            ShowError("Connection failed.");
        }
    }
}