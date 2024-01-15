async function Save() {
    try {
        let response = await fetch("/api[PATH_PREFIX]/settings/theme"
            + "?f=" + document.querySelector("#font").value
            + "&b=" + document.querySelector("#background").value
            + "&a=" + document.querySelector("#accent").value
            + "&d=" + document.querySelector("#design").value);
        if (response.status === 200)
            window.location.reload();
        else ShowError("Connection failed.");
    } catch {
        ShowError("Connection failed.");
    }
}