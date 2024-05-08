let username = document.querySelector("#username");
let email = document.querySelector("#email");
let password1 = document.querySelector("#password1");
let password2 = document.querySelector("#password2");
let continueButton = document.querySelector("#continueButton").firstElementChild;

async function Continue() {
    HideError();
    if (username.value === "") {
        ShowError("Enter a username.");
    } else if (email.value === "") {
        ShowError("Enter your email address.");
    } else if (password1.value === "") {
        ShowError("Enter a password.");
    } else if (password2.value === "") {
        ShowError("Confirm the password.");
    } else if (password1.value != password2.value) {
        ShowError("The passwords do not match.");
    } else {
        continueButton.innerText = "Loading...";
        let response = await fetch("/api[PATH_PREFIX]/register?username=" + encodeURIComponent(username.value) + "&email=" + encodeURIComponent(email.value) + "&password=" + encodeURIComponent(password1.value));
        if (response.status === 200) {
            let text = await response.text();
            switch (text) {
                case "ok": Redirect(); break;
                case "bad-username": ShowError("Usernames must be at least 3 characters long and only contain lowercase letters, digits, dashes, dots and underscores. The first and last characters can only be letters or digits."); break;
                case "bad-password": ShowError("Passwords must be at least 8 characters long and contain at least one uppercase letter, lowercase letter, digit and special character."); break;
                case "bad-email": ShowError("Invalid email address."); break;
                case "username-exists": ShowError("This username is already being used by another account."); break;
                case "email-exists": ShowError("This email address is already being used by another account."); break;
                default: ShowError("Connection failed.");
            }
        } else {
            ShowError("Connection failed.");
        }
        continueButton.innerText = "Continue";
    }
}