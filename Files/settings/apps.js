async function Remove(index, name, expires) {
    await fetch("/api[PATH_PREFIX]/settings/remove-limited-token?index=" + index + "&name=" + name + "&expires=" + expires);
    window.location.reload();
}