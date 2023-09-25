using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public static async Task Other(ApiRequest request, string path, string pathPrefix)
    {
        switch (path)
        {
            case "/login":
                if (await AlreadyLoggedIn(request)) break;
                if (request.Query.ContainsKey("username") && request.Query.ContainsKey("password"))
                {
                    string username = request.Query["username"], password = request.Query["password"];
                    User? user = request.UserTable.Login(username, password, request);
                    await request.Write(user == null ? "no" : "ok");
                    if (user != null && !user.TwoFactor.TOTPEnabled()) Presets.WarningMail(user, "New login", "Someone just successfully logged into your account.");
                }
                else request.Status = 400;
                break;
            case "/register":
                if (await AlreadyLoggedIn(request)) break;
                if (request.Query.ContainsKey("username") && request.Query.ContainsKey("email") && request.Query.ContainsKey("password"))
                {
                    string username = request.Query["username"], email = request.Query["email"], password = request.Query["password"];
                    try
                    {
                        User user = request.UserTable.Register(username, email, password, request);
                        Presets.WarningMail(user, "Welcome", $"Thank you for registering on <a href=\"{request.Context.ProtoHost()}\">{request.Domain}</a>.\nTo verify your email address, click <a href=\"{request.Context.ProtoHost()}{pathPrefix}/verify?code={user.MailToken}\">here</a> or enter the following code: {user.MailToken}");
                        await request.Write("ok");
                    }
                    catch (Exception ex)
                    {
                        await request.Write(ex.Message switch
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
                else request.Status = 400;
                break;
            case "/verify":
                if (request.User == null)
                    await request.Write("Not logged in.");
                else
                {
                    User user = request.User;
                    if (user.MailToken == null)
                        await request.Write("Already verified.");
                    else
                    {
                        if (request.Query.TryGet("resend") == "please")
                        {
                            Presets.WarningMail(user, "Welcome", $"Thank you for registering on <a href=\"{request.Context.ProtoHost()}\">{request.Domain}</a>.\nTo verify your email address, click <a href=\"{request.Context.ProtoHost()}{pathPrefix}/verify?code={user.MailToken}\">here</a> or enter the following code: {user.MailToken}");
                        }
                        else if (!request.Query.ContainsKey("code")) request.Status = 400;
                        else if (user.VerifyMail(request.Query["code"], request))
                            await request.Write("ok");
                        else await request.Write("Invalid code.");
                    }
                }
                break;
            case "/verify-change":
                {
                    if (request.User == null)
                    {
                        await request.Write("Not logged in.");
                        break;
                    }
                    User user = request.User;
                    if (user.MailToken == null)
                    {
                        await request.Write("Already verified.");
                        break;
                    }
                    if (!request.Query.ContainsKey("email"))
                    {
                        request.Status = 400;
                        break;
                    }
                    string mail = request.Query["email"];
                    try
                    {
                        user.SetMailAddress(mail, request.UserTable);
                        user.SetNewMailToken();
                        Presets.WarningMail(user, "Welcome", $"Thank you for registering on <a href=\"{request.Context.ProtoHost()}\">{request.Domain}</a>.\nTo verify your email address, click <a href=\"{request.Context.ProtoHost()}{pathPrefix}/verify?code={user.MailToken}\">here</a> or enter the following code: {user.MailToken}");
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
                break;
            case "/2fa":
                if (request.LoginState != LoginState.Needs2FA || request.User == null)
                {
                    await request.Write("User does not need 2FA verification at the moment.");
                    break;
                }
                else if (!request.Query.ContainsKey("code"))
                {
                    request.Status = 400;
                    break;
                }
                else
                {
                    string code = request.Query["code"];
                    User user = request.User;
                    if (!user.TwoFactor.TOTPEnabled(out var totp)) request.Status = 404;
                    else if (totp.Validate(code, request, true))
                    {
                        await request.Write("ok");
                        Presets.WarningMail(user, "New login", "Someone just successfully logged into your account.");
                    }
                    else await request.Write("no");
                }
                break;
            default:
                request.Status = 404;
                break;
        }
    }
}