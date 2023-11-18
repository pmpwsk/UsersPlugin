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
                    if (!request.Query.TryGetValue("email", out var email))
                    {
                        request.Status = 400;
                        break;
                    }
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
                {
                    if (request.Query.TryGetValue("email", out var email))
                    {
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
                    else if (request.Query.TryGetValue("password", out var password) && request.Query.TryGetValue("token", out var token) && token.Length == 30)
                    {
                        string id = token.Remove(12);
                        string code = token.Remove(0, 12);
                        var users = request.UserTable;
                        if (users.TryGetValue(id, out var user) && user.Settings.TryGetValue("PasswordReset", out var existingCode))
                        {
                            if (existingCode != code)
                            {
                                AccountManager.ReportFailedAuth(request.Context);
                                request.Status = 400;
                                break;
                            }
                            try
                            {
                                user.SetPassword(password);
                                Presets.WarningMail(user, "Password recovery", "Your password was just changed by recovery.");
                                user.Settings.Delete("PasswordReset");
                                await request.Write("ok");
                            }
                            catch (Exception ex)
                            {
                                await request.Write(ex.Message switch
                                {
                                    "Invalid password format." => "bad",
                                    "The provided password is the same as the old one." => "same",
                                    _ => "error"
                                });
                            }
                        }
                        else request.Status = 400;
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