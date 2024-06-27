let key = document.querySelector("#key");
let val = document.querySelector("#value");
let del = document.querySelector("#delete");
let access = document.querySelector("#access-level");
let save = document.querySelector("#save");

async function SetSetting(id) {
    HideError();
    if (key.value === "") {
        ShowError("Enter a key.");
    } else if (val.value === "") {
        ShowError("Enter a value.");
    } else
        switch (await SendRequest(`user/settings/set?id=${id}&key=${encodeURIComponent(key.value)}&value=${encodeURIComponent(val.value)}`, "POST", true)) {
            case 200: window.location.reload(); break;
            default: ShowError("Connection failed."); break;
        }
}

async function DeleteSetting(id, k) {
    HideError();
    switch (await SendRequest(`user/settings/delete?id=${id}&key=${encodeURIComponent(k)}`, "POST", true)) {
        case 200: window.location.reload(); break;
        default: ShowError("Connection failed."); break;
    }
}

async function DeleteUser(id) {
    HideError();
    if (del.firstElementChild.textContent !== "Are you sure?") {
        del.firstElementChild.textContent = "Are you sure?";
    } else
        switch (await SendRequest(`user/delete?id=${id}`, "POST", true)) {
            case 200: window.location.assign("../users"); break;
            default: ShowError("Connection failed."); break;
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
        switch (await SendRequest(`user/set-access-level?id=${id}&value=${access.value}`, "POST", true)) {
            case 200:
                save.className = "";
                save.innerText = "Saved!";
                return;
            case 400: ShowError("The access level needs to be a number between 1 and 65535!"); break;
            default: ShowError("Connection failed."); break;
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