async function Allow() {
    try {
        var background = GetQuery("background");
        if (background !== "null") {
            var result = await GetReturnAddress(background);
            if (result !== null) {
                if ((await fetch(result)).status === 200) {
                    window.location.assign(GetQuery("yes"));
                    return;
                }
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
        var response = await fetch("/api[PATH_PREFIX]/generate-limited-token?return=" + encodeURIComponent(address) + "&name=" + encodeURIComponent(GetQuery("name")) + "&allowed=" + encodeURIComponent(GetQuery("allowed")));
        if (response.status === 200)
            return await response.text();
    } catch {
    }

    return null;
}

function GetQuery(q) {
    try {
        let query = new URLSearchParams(window.location.search);
        if (query.has(q)) {
            return query.get(q);
        } else {
            return "null";
        }
    } catch {
        return "null";
    }
}