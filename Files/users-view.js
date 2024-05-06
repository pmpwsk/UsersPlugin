let key = document.querySelector("#key");
let val = document.querySelector("#value");
let del = document.querySelector("#delete");
let access = document.querySelector("#access-level");
let save = document.querySelector("#save");

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

async function SaveAccessLevel(id) {
    HideError();
    var value = parseInt(access.value);
    if (value == NaN || value < 1 || value > 65535) {
        ShowError("The access level needs to be a number between 1 and 65535!");
        return;
    }
    save.className = "green";
    save.innerText = "Saving...";
    try {
        switch ((await fetch("/api[PATH_PREFIX]/users/set-access-level?id=" + id + "&value=" + access.value)).status) {
            case 200:
                save.className = "";
                save.innerText = "Saved!";
                return;
            case 400:
                ShowError("The access level needs to be a number between 1 and 65535!");
                break;
            default:
                ShowError("Connection failed.");
                break;
        }
    } catch {
        ShowError("Connection failed.");
    }
    save.className = "green";
    save.innerText = "Save";
}

async function SetAccessLevel(id, value) {
    access.value = value;
    SaveAccessLevel(id);
}

async function AccessLevelChanged() {
    save.className = "green";
    save.innerText = "Save";
}