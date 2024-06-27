async function Save() {
    HideError();
    switch (await SendRequest("theme/set"
        + "?f=" + document.querySelector("#font").value
        + "&b=" + document.querySelector("#background").value
        + "&a=" + document.querySelector("#accent").value
        + "&d=" + document.querySelector("#design").value, "POST", true)) {
        case 200: window.location.reload(); break;
        default: ShowError("Connection failed."); break;
    }
}