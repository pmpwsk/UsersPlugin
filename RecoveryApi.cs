using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public static async Task Recovery(ApiRequest request, string path, string pathPrefix)
    {
        if (await AlreadyLoggedIn(request)) return;
        switch (path)
        {
            case "/username":
                {
                    if (!request.Query.ContainsKey("email"))
                    {
                        request.Status = 400;
                        break;
                    }
                    string email = request.Query["email"];
                    User? user = request.UserTable.FindByMailAddress(email);
                    if (user == null)
                    {
                        AccountManager.ReportFailedAuth(request.Context);
                        await request.Write("no");
                        break;
                    }
                    Presets.WarningMail(user, "Username recovery", $"You requested your username, it is: {user.Username}");
                    await request.Write("ok");
                }
                break;
            case "/password":
                if (request.Query.ContainsKey("email"))
                {
                    string email = request.Query["email"];
                    User? user = request.UserTable.FindByMailAddress(email);
                        if (user == null)
                    {
                        AccountManager.ReportFailedAuth(request.Context);
                        await request.Write("no");
                        break;
                    }
                    string code = Parsers.RandomString(18);
                    user.Settings["PasswordReset"] = code;
                    string url = $"{request.Context.ProtoHost()}{pathPrefix}/recovery/password?token={user.Id}{code}";
                    Presets.WarningMail(user, "Password recovery", $"You requested password recovery. Open the following link to reset your password:\n<a href=\"{url}\">{url}</a>\nYou can cancel the password reset from Account > Settings > Password.");
                    await request.Write("ok");
                }
                else if (request.Query.ContainsKeys("password", "token"))
                {
                    string token = request.Query["token"];
                    if (token.Length == 30)
                    {
                        string id = token.Remove(12);
                        string code = token.Remove(0, 12);
                        var users = request.UserTable;
                        if (users.ContainsKey(id))
                        {
                            User user = users[id];
                            if (user.Settings.ContainsKey("PasswordReset") && !user.Settings.ContainsKey("Delete"))
                            {
                                if (user.Settings["PasswordReset"] != code)
                                {
                                    AccountManager.ReportFailedAuth(request.Context);
                                }
                                else
                                {
                                    string password = request.Query["password"];
                                    try
                                    {
                                        user.SetPassword(password);
                                        Presets.WarningMail(user, "Password recovery", "Your password was just changed by recovery.");
                                        user.Settings.Delete("PasswordReset");
                                        await request.Write("ok");
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        await request.Write(ex.Message switch
                                        {
                                            "Invalid password format." => "bad",
                                            "The provided password is the same as the old one." => "same",
                                            _ => "error"
                                        });
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    request.Status = 400;
                }
                else request.Status = 400;
                break;
            default:
                request.Status = 404;
                break;
        }
    }
}