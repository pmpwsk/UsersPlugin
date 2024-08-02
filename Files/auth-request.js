async function Allow() {
    HideError();
    try {
        var background = GetQuery("background");
        if (background !== "null") {
            var result = await GetReturnAddress(background);
            if (result !== null)
                if ((await fetch(result, {method:"POST"})).status === 200) {
                    window.location.assign(GetQuery("yes"));
                    return;
                }
        } else {
            var result = await GetReturnAddress(GetQuery("yes"));
            if (result !== null) {
                window.location.assign(result);
                return;
            }
        }
    } catch {
    }

    ShowError("Connection failed.");
}

async function GetReturnAddress(address) {
    try {
        var response = await fetch(`auth-request/generate-limited-token?return=${encodeURIComponent(address)}&name=${encodeURIComponent(GetQuery("name"))}&allowed=${encodeURIComponent(GetQuery("allowed"))}`, {method:"POST"});
        if (response.status === 200)
            return await response.text();
    } catch {
    }

    return null;
}

function GetQuery(q) {
    try {
        var query = new URLSearchParams(window.location.search);
        return query.has(q) ? query.get(q) : "null";
    } catch {
        return "null";
    }
}