using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public async Task Settings(ApiRequest req, string path, string pathPrefix)
    {
        if (await NotLoggedIn(req))
            return;
        switch (path)
        {
            case "/2fa":
                {
                    if (req.Query.TryGetValue("method", out var method) && req.Query.TryGetValue("code", out var code) && req.Query.TryGetValue("password", out var password))
                    {
                        if (req.User.TwoFactor.TOTP == null)
                            await req.Write("2FA not enabled.");
                        else if (!req.User.ValidatePassword(password, req))
                            await req.Write("no");
                        else if (!req.User.TwoFactor.TOTP.Validate(code, req, method != "enable"))
                            await req.Write("no");
                        else
                        {
                            switch (method)
                            {
                                case "enable":
                                    if (req.User.TwoFactor.TOTP.Verified)
                                        await req.Write("2FA already enabled.");
                                    else
                                    {
                                        req.User.TwoFactor.TOTP.Verify();
                                        Presets.WarningMail(req.User, "2FA enabled", "Two-factor authentication has just been enabled.");
                                        await req.Write("ok");
                                    }
                                    break;
                                case "disable":
                                    req.User.TwoFactor.DisableTOTP();
                                    Presets.WarningMail(req.User, "2FA disabled", "Two-factor authentication has just been disabled.");
                                    await req.Write("ok");
                                    break;
                                default:
                                    req.Status = 400;
                                    break;
                            }
                        }
                    }
                    else req.Status = 400;
                }
                break;
            case "/theme":
                {
                    ThemeFromQuery(req.Context.Request.QueryString.Value ?? "", out string font, out _, out string background, out string accent, out string design);
                    req.User.Settings["Theme"] = $"?f={font}&b={background}&a={accent}&d={design}";
                }
                break;
            case "/username":
                {
                    if (!req.Query.TryGetValue("username", out var username))
                    {
                        req.Status = 400;
                        break;
                    }
                    if (!await req.Auth(req.User))
                        break;
                    try
                    {
                        string oldUsername = req.User.Username;
                        req.User.SetUsername(username, req.UserTable);
                        Presets.WarningMail(req.User, "Username changed", $"Your username was just changed from {oldUsername} to {username}.");
                        await req.Write("ok");
                    }
                    catch (Exception ex)
                    {
                        await req.Write(ex.Message switch
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
                    if (req.Query.TryGet("action") == "cancel")
                    {
                        req.User.Settings.Delete("PasswordReset");
                        break;
                    }
                    if (!req.Query.TryGetValue("new-password", out var password))
                    {
                        req.Status = 400;
                        break;
                    }
                    if (!await req.Auth(req.User))
                        break;
                    try
                    {
                        req.User.SetPassword(password);
                        Presets.WarningMail(req.User, "Password changed", "Your password was just changed.");
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
                break;
            case "/email":
                {
                    if (req.User.Settings.TryGetValue("EmailChange", out var settingRaw))
                    {
                        string[] setting = settingRaw.Split('&');
                        string mail = HttpUtility.UrlDecode(setting[0]);
                        string existingCode = setting[1];
                        if (req.Query.TryGet("resend") == "please")
                            Presets.WarningMail(req.User, "Email change", $"You requested to change your email address to this address. Your verification code is: {existingCode}", mail);
                        else if (req.Query.TryGetValue("code", out var code))
                        {
                            if (code != existingCode)
                            {
                                AccountManager.ReportFailedAuth(req.Context);
                                await req.Write("no");
                                break;
                            }
                            try
                            {
                                string oldMail = req.User.MailAddress;
                                req.User.SetMailAddress(mail, req.UserTable);
                                Presets.WarningMail(req.User, "Email changed", $"Your email was just changed from {oldMail} to {mail}.", oldMail);
                                req.User.Settings.Delete("EmailChange");
                                await req.Write("ok");
                            }
                            catch (Exception ex)
                            {
                                await req.Write(ex.Message switch
                                {
                                    "Another user with the provided mail address already exists." => "exists",
                                    "The provided mail address is the same as the old one." => "same",
                                    "Invalid mail address format." => "bad",
                                    _ => "error"
                                });
                            }
                        }
                        else req.Status = 400;
                    }
                    else
                    {
                        if (!await req.Auth(req.User))
                            break;
                        if (!req.Query.TryGetValue("email", out var email))
                            req.Status = 400;
                        else if (req.User.MailAddress == email)
                            await req.Write("same");
                        else if (!AccountManager.CheckMailAddressFormat(email))
                            await req.Write("bad");
                        else if (req.UserTable.FindByMailAddress(email) != null)
                            await req.Write("exists");
                        else
                        {
                            string code = Parsers.RandomString(10);
                            req.User.Settings["EmailChange"] = $"{HttpUtility.UrlEncode(email)}&{code}";
                            Presets.WarningMail(req.User, "Email change", $"You requested to change your email address to this address. Your verification code is: {code}", email);
                            await req.Write("ok");
                        }
                    }
                }
                break;
            case "/delete":
                {
                    if (!await req.Auth(req.User))
                        break;
                    req.Cookies.Delete("AuthToken");
                    req.User.Auth.DeleteAll();
                    req.User.Settings["Delete"] = DateTime.UtcNow.Ticks.ToString();
                    Presets.WarningMail(req.User, "Account deletion", "You just requested your account to be deleted. We will keep your data for another 30 days, in case you change your mind. If you want to restore your account, simply log in again within the next 30 days. If you want us to delete your data immediately, please contact us by replying to this email.");
                    await req.Write("ok");
                }
                break;
            case "/remove-limited-token":
                {
                    if (req.Query.TryGetValue("index", out int index) && index >= 0 && req.Query.TryGetValue("name", out var name) && req.Query.TryGetValue("expires", out long ticks))
                    {
                        var kv = req.User.Auth.ElementAtOrDefault(index);
                        if (kv.Equals(default(KeyValuePair<string, AuthTokenData>)))
                            req.Status = 404;
                        else if (kv.Value.FriendlyName == name && kv.Value.Expires.Ticks == ticks)
                            req.User.Auth.Delete(kv.Key);
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