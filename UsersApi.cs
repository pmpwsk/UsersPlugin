using uwap.WebFramework.Accounts;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public static async Task Users(ApiRequest request, string path, string pathPrefix)
    {
        if (!request.IsAdmin())
        {
            request.Status = 403;
            return;
        }
        switch (path)
        {
            case "/set-setting":
                {
                    if (request.Query.TryGetValue("id", out var id) && request.Query.TryGetValue("key", out var key) && request.Query.TryGetValue("value", out var value))
                    {
                        if (!request.UserTable.TryGetValue(id, out var user))
                        {
                            request.Status = 404;
                            break;
                        }
                        user.Settings[key] = value;
                        await request.Write("ok");
                    }
                    else request.Status = 400;
                }
                break;
            case "/delete-setting":
                {
                    if (request.Query.TryGetValue("id", out var id) && request.Query.TryGetValue("key", out var key))
                    {
                        if (!request.UserTable.TryGetValue(id, out var user))
                        {
                            request.Status = 404;
                            break;
                        }
                        user.Settings.Delete(key);
                        await request.Write("ok");
                    }
                    else request.Status = 400;
                }
                break;
            case "/delete-user":
                {
                    if (request.Query.TryGetValue("id", out var id))
                    {
                        if (request.UserTable.Delete(id))
                            await request.Write("ok");
                        else request.Status = 404;
                    }
                    else request.Status = 400;
                }
                break;
            default:
                request.Status = 404;
                break;
        }
    }
}