using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public static async Task Other(ApiRequest req, string path, string pathPrefix)
    {
        switch (path)
        {
            case "/login":
                if (!await AlreadyLoggedIn(req))
                    if (req.Query.TryGetValue("username", out var username) && req.Query.TryGetValue("password", out var password))
                    {
                        User? user = req.UserTable.Login(username, password, req);
                        if (user != null)
                        {
                            user.Settings.Delete("Delete");
                            if (!user.TwoFactor.TOTPEnabled())
                                Presets.WarningMail(user, "New login", "Someone just successfully logged into your account.");
                            await req.Write("ok");
                        }
                        else await req.Write("no");
                    }
                    else req.Status = 400;
                break;
            case "/register":
                if (!await AlreadyLoggedIn(req))
                    if (req.Query.TryGetValue("username", out var username) && req.Query.TryGetValue("email", out var email) && req.Query.TryGetValue("password", out var password))
                    {
                        try
                        {
                            User user = req.UserTable.Register(username, email, password, req);
                            Presets.WarningMail(user, "Welcome", $"Thank you for registering on <a href=\"{req.Context.ProtoHost()}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.Context.ProtoHost()}{pathPrefix}/verify?code={user.MailToken}&user={user.Id}\">here</a> or enter the following code: {user.MailToken}");
                            await req.Write("ok");
                        }
                        catch (Exception ex)
                        {
                            await req.Write(ex.Message switch
                            {
                                "Invalid username format." => "bad-username",
                                "Invalid mail address format." => "bad-email",
                                "Invalid password format." => "bad-password",
                                "Another user with the provided username already exists." => "username-exists",
                                "Another user with the provided email address already exists." => "email-exists",
                                _ => "Error 500: Internal server error."
                            });
                        }
                    }
                    else req.Status = 400;
                break;
            case "/verify":
                {
                    if (!req.HasUser)
                        await req.Write("Not logged in.");
                    else if (req.User.MailToken == null)
                        await req.Write("Already verified.");
                    else if (req.Query.TryGet("resend") == "please")
                        Presets.WarningMail(req.User, "Welcome", $"Thank you for registering on <a href=\"{req.Context.ProtoHost()}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.Context.ProtoHost()}{pathPrefix}/verify?code={req.User.MailToken}\">here</a> or enter the following code: {req.User.MailToken}");
                    else if (!req.Query.TryGetValue("code", out var code))
                        req.Status = 400;
                    else if (req.User.VerifyMail(code, req))
                        await req.Write("ok");
                    else await req.Write("Invalid code.");
                }
                break;
            case "/verify-change":
                {
                    if (!req.HasUser)
                    {
                        await req.Write("Not logged in.");
                        break;
                    }
                    if (req.User.MailToken == null)
                    {
                        await req.Write("Already verified.");
                        break;
                    }
                    if (!req.Query.TryGetValue("email", out var email))
                    {
                        req.Status = 400;
                        break;
                    }
                    string mail = email;
                    try
                    {
                        req.User.SetMailAddress(mail, req.UserTable);
                        req.User.SetNewMailToken();
                        Presets.WarningMail(req.User, "Welcome", $"Thank you for registering on <a href=\"{req.Context.ProtoHost()}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.Context.ProtoHost()}{pathPrefix}/verify?code={req.User.MailToken}\">here</a> or enter the following code: {req.User.MailToken}");
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
                break;
            case "/2fa":
                {
                    if (req.LoginState != LoginState.Needs2FA)
                        await req.Write("User does not need 2FA verification at the moment.");
                    else if (!req.Query.TryGetValue("code", out var code))
                        req.Status = 400;
                    else if (!req.User.TwoFactor.TOTPEnabled(out var totp))
                        req.Status = 404;
                    else if (totp.Validate(code, req, true))
                    {
                        await req.Write("ok");
                        Presets.WarningMail(req.User, "New login", "Someone just successfully logged into your account.");
                    }
                    else await req.Write("no");
                }
                break;
            case "/get-username":
                if (!await NotLoggedIn(req))
                    await req.Write(req.User.Username);
                break;
            case "/generate-limited-token":
                if (!await NotLoggedIn(req))
                {
                    if (req.Query.TryGetValue("name", out var name) && name != "" && name == name.HtmlSafe() && req.Query.TryGetValue("return", out var returnAddress) && req.Query.TryGetValue("allowed", out var limitedToPathsEncoded))
                    {
                        var limitedToPaths =
                        ((IEnumerable<string>)[
                            ..limitedToPathsEncoded.Split(',').Select(x => HttpUtility.UrlDecode(x).HtmlSafe()),
                            $"{req.Domain}{pathPrefix}/logout"
                        ]).ToList().AsReadOnly();
                        if (limitedToPaths.Contains(""))
                        {
                            req.Status = 400;
                            break;
                        }

                        string token = req.User.Auth.AddNewLimited(name, limitedToPaths);
                        AccountManager.GenerateAuthTokenCookieOptions(out var expires, out var sameSite, out var domain, req.Context);
                        await req.Write(returnAddress
                            .Replace("[TOKEN]", req.User.Id + token)
                            .Replace("[EXPIRES]", expires.Ticks.ToString())
                            .Replace("[SAMESITE]", sameSite.ToString())
                            .Replace("[DOMAIN]", domain));
                        Presets.WarningMail(req.User, $"App '{name}' was granted access", $"The app '{name}' has just been granted limited access to your account. You can view and manage apps with access in your account settings.");
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