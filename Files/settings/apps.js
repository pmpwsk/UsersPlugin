async function Remove(index, name, expires) {
    if ((await fetch(`apps/remove?index=${index}&name=${name}&expires=${expires}`, {method:"POST"})).status === 200)
        window.location.reload();
    else ShowError("Connection failed.");
}