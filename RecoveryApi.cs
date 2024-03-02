using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public static async Task Recovery(ApiRequest req, string path, string pathPrefix)
    {
        if (await AlreadyLoggedIn(req))
            return;
        switch (path)
        {
            case "/username":
                {
                    if (!req.Query.TryGetValue("email", out var email))
                    {
                        req.Status = 400;
                        break;
                    }
                    User? user = req.UserTable.FindByMailAddress(email);
                    if (user == null)
                    {
                        AccountManager.ReportFailedAuth(req.Context);
                        await req.Write("no");
                        break;
                    }
                    Presets.WarningMail(user, "Username recovery", $"You requested your username, it is: {user.Username}");
                    await req.Write("ok");
                }
                break;
            case "/password":
                {
                    if (req.Query.TryGetValue("email", out var email))
                    {
                        User? user = req.UserTable.FindByMailAddress(email);
                        if (user == null)
                        {
                            AccountManager.ReportFailedAuth(req.Context);
                            await req.Write("no");
                            break;
                        }
                        string code = Parsers.RandomString(18);
                        user.Settings["PasswordReset"] = code;
                        string url = $"{req.Context.ProtoHost()}{pathPrefix}/recovery/password?token={user.Id}{code}";
                        Presets.WarningMail(user, "Password recovery", $"You requested password recovery. Open the following link to reset your password:\n<a href=\"{url}\">{url}</a>\nYou can cancel the password reset from Account > Settings > Password.");
                        await req.Write("ok");
                    }
                    else if (req.Query.TryGetValue("password", out var password) && req.Query.TryGetValue("token", out var token) && token.Length == 30)
                    {
                        string id = token.Remove(12);
                        string code = token.Remove(0, 12);
                        var users = req.UserTable;
                        if (users.TryGetValue(id, out var user) && user.Settings.TryGetValue("PasswordReset", out var existingCode))
                        {
                            if (existingCode != code)
                            {
                                AccountManager.ReportFailedAuth(req.Context);
                                req.Status = 400;
                                break;
                            }
                            try
                            {
                                user.SetPassword(password);
                                Presets.WarningMail(user, "Password recovery", "Your password was just changed by recovery.");
                                user.Settings.Delete("PasswordReset");
                                await req.Write("ok");
                            }
                            catch (Exception ex)
                            {
                                await req.Write(ex.Message switch
                                {
                                    "Invalid password format." => "bad",
                                    "The provided password is the same as the old one." => "same",
                                    _ => "error"
                                });
                            }
                        }
                        else req.Status = 400;
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