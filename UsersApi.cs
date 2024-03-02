namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public static async Task Users(ApiRequest req, string path, string pathPrefix)
    {
        if (!req.IsAdmin())
        {
            req.Status = 403;
            return;
        }
        switch (path)
        {
            case "/set-setting":
                {
                    if (req.Query.TryGetValue("id", out var id) && req.Query.TryGetValue("key", out var key) && req.Query.TryGetValue("value", out var value))
                    {
                        if (!req.UserTable.TryGetValue(id, out var user))
                        {
                            req.Status = 404;
                            break;
                        }
                        user.Settings[key] = value;
                        await req.Write("ok");
                    }
                    else req.Status = 400;
                }
                break;
            case "/delete-setting":
                {
                    if (req.Query.TryGetValue("id", out var id) && req.Query.TryGetValue("key", out var key))
                    {
                        if (!req.UserTable.TryGetValue(id, out var user))
                        {
                            req.Status = 404;
                            break;
                        }
                        user.Settings.Delete(key);
                        await req.Write("ok");
                    }
                    else req.Status = 400;
                }
                break;
            case "/delete-user":
                {
                    if (req.Query.TryGetValue("id", out var id))
                    {
                        if (req.UserTable.Delete(id))
                            await req.Write("ok");
                        else req.Status = 404;
                    }
                    else req.Status = 400;
                }
                break;
            case "/set-access-level":
                {
                    if (req.Query.TryGetValue("id", out string? id) && req.Query.TryGetValue("value", out ushort value))
                    {
                        if (req.UserTable.TryGetValue(id, out var user))
                            user.AccessLevel = value;
                        else req.Status = 404;
                    }
                    else req.Status = 400;
                }
                break;
            default:
                req.Status = 404;
                break;
        }
    }
}