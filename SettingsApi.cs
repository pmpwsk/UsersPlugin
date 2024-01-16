using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public async Task Settings(ApiRequest request, string path, string pathPrefix)
    {
        if (await NotLoggedIn(request)) return;
        User user = request.User;
        switch (path)
        {
            case "/2fa":
                {
                    if (request.Query.TryGetValue("method", out var method) && request.Query.TryGetValue("code", out var code) && request.Query.TryGetValue("password", out var password))
                    {
                        if (user.TwoFactor.TOTP == null)
                            await request.Write("2FA not enabled.");
                        else if (!user.ValidatePassword(password, request))
                            await request.Write("no");
                        else if (!user.TwoFactor.TOTP.Validate(code, request, method != "enable"))
                            await request.Write("no");
                        else
                        {
                            switch (method)
                            {
                                case "enable":
                                    if (user.TwoFactor.TOTP.Verified) await request.Write("2FA already enabled.");
                                    else
                                    {
                                        user.TwoFactor.TOTP.Verify();
                                        Presets.WarningMail(user, "2FA enabled", "Two-factor authentication has just been enabled.");
                                        await request.Write("ok");
                                    }
                                    break;
                                case "disable":
                                    user.TwoFactor.DisableTOTP();
                                    Presets.WarningMail(user, "2FA disabled", "Two-factor authentication has just been disabled.");
                                    await request.Write("ok");
                                    break;
                                default:
                                    request.Status = 400;
                                    break;
                            }
                        }
                    }
                    else request.Status = 400;
                }
                break;
            case "/theme":
                {
                    if (request.Query.TryGetValue("name", out var name) && Presets.Themes.Contains(name))
                    {
                        if (name != Presets.ThemeName(request))
                            user.Settings["Theme"] = name;
                    }
                    else request.Status = 400;
                }
                break;
            case "/username":
                {
                    if (!request.Query.TryGetValue("username", out var username))
                    {
                        request.Status = 400;
                        break;
                    }
                    if (!await request.Auth(user))
                        break;
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
                break;
            case "/password":
                {
                    if (request.Query.TryGet("action") == "cancel")
                    {
                        user.Settings.Delete("PasswordReset");
                        break;
                    }
                    if (!request.Query.TryGetValue("new-password", out var password))
                    {
                        request.Status = 400;
                        break;
                    }
                    if (!await request.Auth(user))
                        break;
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
                break;
            case "/email":
                {
                    if (user.Settings.TryGetValue("EmailChange", out var settingRaw))
                    {
                        string[] setting = settingRaw.Split('&');
                        string mail = HttpUtility.UrlDecode(setting[0]);
                        string existingCode = setting[1];
                        if (request.Query.TryGet("resend") == "please")
                            Presets.WarningMail(user, "Email change", $"You requested to change your email address to this address. Your verification code is: {existingCode}", mail);
                        else if (request.Query.TryGetValue("code", out var code))
                        {
                            if (code != existingCode)
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
                        if (!await request.Auth(user))
                            break;
                        if (!request.Query.TryGetValue("email", out var email))
                            request.Status = 400;
                        else if (user.MailAddress == email)
                            await request.Write("same");
                        else if (!AccountManager.CheckMailAddressFormat(email))
                            await request.Write("bad");
                        else if (request.UserTable.FindByMailAddress(email) != null)
                            await request.Write("exists");
                        else
                        {
                            string code = Parsers.RandomString(10);
                            user.Settings["EmailChange"] = $"{HttpUtility.UrlEncode(email)}&{code}";
                            Presets.WarningMail(user, "Email change", $"You requested to change your email address to this address. Your verification code is: {code}", email);
                            await request.Write("ok");
                        }
                    }
                }
                break;
            case "/delete":
                {
                    if (!await request.Auth(user))
                        break;
                    request.Cookies.Delete("AuthToken");
                    user.Auth.DeleteAll();
                    user.Settings["Delete"] = DateTime.UtcNow.Ticks.ToString();
                    Presets.WarningMail(user, "Account deletion", "You just requested your account to be deleted. We will keep your data for another 30 days, in case you change your mind. If you want to restore your account, simply log in again within the next 30 days. If you want us to delete your data immediately, please contact us by replying to this email.");
                    await request.Write("ok");
                }
                break;
            default:
                request.Status = 404;
                break;
        }
    }
}