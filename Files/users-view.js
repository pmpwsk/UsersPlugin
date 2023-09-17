let key = document.querySelector("#key");
let val = document.querySelector("#value");
let del = document.querySelector("#delete");

async function SetSetting(id) {
    if (key.value === "") {
        ShowError("Enter a key.");
    } else if (val.value === "") {
        ShowError("Enter a value.");
    } else {
        let response = await fetch("/api[PATH_PREFIX]/users/set-setting?id=" + id + "&key=" + encodeURIComponent(key.value) + "&value=" + encodeURIComponent(val.value));
        if (response.status === 200) {
            let text = await response.text();
            switch (text) {
                case "ok": window.location.reload(); break;
                default: ShowError("Connection failed.");
            }
        } else {
            ShowError("Connection failed.");
        }
    }
}

async function DeleteSetting(id, k) {
    let response = await fetch("/api[PATH_PREFIX]/users/delete-setting?id=" + id + "&key=" + encodeURIComponent(k));
    if (response.status === 200) {
        let text = await response.text();
        switch (text) {
            case "ok": window.location.reload(); break;
            default: ShowError("Connection failed.");
        }
    } else {
        ShowError("Connection failed.");
    }
}

async function DeleteUser(id) {
    if (del.firstElementChild.textContent === "Are you sure?") {
        let response = await fetch("/api[PATH_PREFIX]/users/delete-user?id=" + id);
        if (response.status === 200) {
            let text = await response.text();
            switch (text) {
                case "ok": window.location.assign("[PATH_PREFIX]/users"); break;
                default: ShowError("Connection failed.");
            }
        } else {
            ShowError("Connection failed.");
        }
    }
    else {
        del.firstElementChild.textContent = "Are you sure?";
    }
}