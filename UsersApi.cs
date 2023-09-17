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
                if (!request.Query.ContainsKeys("id", "key", "value"))
                {
                    request.Status = 400;
                    break;
                }
                else
                {
                    string id = request.Query["id"], key = request.Query["key"], value = request.Query["value"];
                    var userTable = request.UserTable;
                    if (!userTable.ContainsKey(id))
                    {
                        request.Status = 404;
                        break;
                    }
                    User user = userTable[id];
                    user.Settings[key] = value;
                    await request.Write("ok");
                }
                break;
            case "/delete-setting":
                if (!request.Query.ContainsKeys("id", "key"))
                {
                    request.Status = 400;
                    break;
                }
                else
                {
                    string id = request.Query["id"], key = request.Query["key"];
                    var userTable = request.UserTable;
                    if (!userTable.ContainsKey(id))
                    {
                        request.Status = 404;
                        break;
                    }
                    User user = userTable[id];
                    user.Settings.Delete(key);
                    await request.Write("ok");
                }
                break;
            case "/delete-user":
                if (!request.Query.ContainsKeys("id"))
                {
                    request.Status = 400;
                }
                else
                {
                    string id = request.Query["id"];
                    var userTable = request.UserTable;

                    if (userTable.Delete(id))
                        await request.Write("ok");
                    else request.Status = 404;
                }
                break;
            default:
                request.Status = 404;
                break;
        }
    }
}