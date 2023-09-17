using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public static async Task Settings(ApiRequest request, string path, string pathPrefix)
    {
        if (await NotLoggedIn(request)) return;
        User user = request.User ?? throw new Exception("Not logged in.");
        switch (path)
        {
            case "/2fa":
                if (!new[] { "method", "code", "password" }.All(x => request.Query.ContainsKey(x)))
                    request.Status = 400;
                else
                {
                    string method = request.Query["method"], code = request.Query["code"], password = request.Query["password"];
                    if (user.TwoFactor == null)
                        await request.Write("2FA not enabled.");
                    else if (!user.ValidatePassword(password, request))
                        await request.Write("no");
                    else if (method == "enable" && user.TwoFactor.Recovery.Contains(code))
                        await request.Write("no");
                    else if (!user.Validate2FA(code, request))
                        await request.Write("no");
                    else
                    {
                        switch (method)
                        {
                            case "enable":
                                if (user.TwoFactor.Verified) await request.Write("2FA already enabled.");
                                else
                                {
                                    user.Verify2FA();
                                    Presets.WarningMail(user, "2FA enabled", "Two-factor authentication has just been enabled.");
                                    await request.Write("ok");
                                }
                                break;
                            case "disable":
                                user.TwoFactor = null;
                                Presets.WarningMail(user, "2FA disabled", "Two-factor authentication has just been disabled.");
                                await request.Write("ok");
                                break;
                            default:
                                request.Status = 400;
                                break;
                        }
                    }
                }
                break;
            case "/theme":
                {
                    if (!request.Query.ContainsKey("name"))
                    {
                        request.Status = 400;
                        break;
                    }
                    string name = request.Query["name"];
                    if (!Presets.Themes.Contains(name))
                    {
                        request.Status = 400;
                        break;
                    }
                    if (name != Presets.ThemeName(request))
                        user.Settings["Theme"] = name;
                }
                break;
            case "/username":
                {
                    if (!request.Query.ContainsKey("username")) { request.Status = 400; }
                    else if (await request.Auth(user))
                    {
                        string username = request.Query["username"];
                        try
                        {
                            string oldUsername = user.Username;
                            user.SetUsername(username, request.UserTable);
                            Presets.WarningMail(user, "Username changed", $"Your username was just changed from {oldUsername} to {username}.");
                            await request.Write("ok");
                        }
                        catch (Exception ex)
                        {
                            await request.Write(ex.Message switch
                            {
                                "Invalid username format." => "bad",
                                "Another user with the provided username already exists." => "exists",
                                "The provided username is the same as the old one." => "same",
                                _ => "error"
                            });
                        }
                    }
                }
                break;
            case "/password":
                {
                    if (request.Query.TryGet("action") == "cancel")
                    {
                        if (user.Settings.ContainsKey("PasswordReset"))
                            user.Settings.Delete("PasswordReset");
                        break;
                    }
                    if (!request.Query.ContainsKey("new-password")) { request.Status = 400; }
                    else if (await request.Auth(user))
                    {
                        string password = request.Query["new-password"];
                        try
                        {
                            user.SetPassword(password);
                            Presets.WarningMail(user, "Password changed", "Your password was just changed.");
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
                }
                break;
            case "/email":
                if (user.Settings.ContainsKey("EmailChange"))
                {
                    string[] setting = user.Settings["EmailChange"].Split('&');
                    string mail = HttpUtility.UrlDecode(setting[0]);
                    string code = setting[1];

                    if (request.Query.TryGet("resend") == "please")
                    {
                        Presets.WarningMail(user, "Email change", $"You requested to change your email address to this address. Your verification code is: {code}", mail);
                    }
                    else if (request.Query.ContainsKey("code"))
                    {
                        if (request.Query["code"] != code)
                        {
                            AccountManager.ReportFailedAuth(request.Context);
                            await request.Write("no");
                            break;
                        }

                        try
                        {
                            string oldMail = user.MailAddress;
                            user.SetMailAddress(mail, request.UserTable);
                            Presets.WarningMail(user, "Email changed", $"Your email was just changed from {oldMail} to {mail}.", oldMail);
                            user.Settings.Delete("EmailChange");
                            await request.Write("ok");
                        }
                        catch (Exception ex)
                        {
                            await request.Write(ex.Message switch
                            {
                                "Another user with the provided mail address already exists." => "exists",
                                "The provided mail address is the same as the old one." => "same",
                                "Invalid mail address format." => "bad",
                                _ => "error"
                            });
                        }
                    }
                    else request.Status = 400;
                }
                else
                {
                    if (await request.Auth(user))
                    {
                        if (!request.Query.ContainsKey("email")) { request.Status = 400; break; }
                        string mail = request.Query["email"];
                        if (user.MailAddress == mail) await request.Write("same");
                        else if (!AccountManager.CheckMailAddressFormat(mail)) await request.Write("bad");
                        else if (request.UserTable.FindByMailAddress(mail) != null) await request.Write("exists");
                        else
                        {
                            string code = Parsers.RandomString(10);
                            user.Settings["EmailChange"] = $"{HttpUtility.UrlEncode(mail)}&{code}";
                            Presets.WarningMail(user, "Email change", $"You requested to change your email address to this address. Your verification code is: {code}", mail);
                            await request.Write("ok");
                        }
                    }
                }
                break;
            case "/delete":
                if (await request.Auth(user))
                {
                    request.Cookies.Delete("AuthToken");
                    user.SetPassword(null);
                    user.Auth.DeleteAll();
                    user.Settings["Delete"] = DateTime.UtcNow.Ticks.ToString();
                    Presets.WarningMail(user, "Account deletion", "You just deleted your account. We will keep your data for another 30 days, in case you change your mind. If you want to restore your account or want us to delete your data immediately, please contact us by replying to this email.");
                    await request.Write("ok");
                }
                break;
            default:
                request.Status = 404;
                break;
        }
    }
}