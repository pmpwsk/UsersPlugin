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
                {
                    if (await AlreadyLoggedIn(request)) break;
                    if (request.Query.TryGetValue("username", out var username) && request.Query.TryGetValue("password", out var password))
                    {
                        User? user = request.UserTable.Login(username, password, request);
                        if (user != null)
                        {
                            user.Settings.Delete("Delete");
                            if (!user.TwoFactor.TOTPEnabled())
                                Presets.WarningMail(user, "New login", "Someone just successfully logged into your account.");
                            await request.Write("ok");
                        }
                        else await request.Write("no");
                    }
                    else request.Status = 400;
                }
                break;
            case "/register":
                {
                    if (await AlreadyLoggedIn(request)) break;
                    if (request.Query.TryGetValue("username", out var username) && request.Query.TryGetValue("email", out var email) && request.Query.TryGetValue("password", out var password))
                    {
                        try
                        {
                            User user = request.UserTable.Register(username, email, password, request);
                            Presets.WarningMail(user, "Welcome", $"Thank you for registering on <a href=\"{request.Context.ProtoHost()}\">{request.Domain}</a>.\nTo verify your email address, click <a href=\"{request.Context.ProtoHost()}{pathPrefix}/verify?code={user.MailToken}&user={user.Id}\">here</a> or enter the following code: {user.MailToken}");
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
                }
                break;
            case "/verify":
                {
                    if (!request.HasUser)
                        await request.Write("Not logged in.");
                    else if (request.User.MailToken == null)
                        await request.Write("Already verified.");
                    else if (request.Query.TryGet("resend") == "please")
                        Presets.WarningMail(request.User, "Welcome", $"Thank you for registering on <a href=\"{request.Context.ProtoHost()}\">{request.Domain}</a>.\nTo verify your email address, click <a href=\"{request.Context.ProtoHost()}{pathPrefix}/verify?code={request.User.MailToken}\">here</a> or enter the following code: {request.User.MailToken}");
                    else if (!request.Query.TryGetValue("code", out var code))
                        request.Status = 400;
                    else if (request.User.VerifyMail(code, request))
                        await request.Write("ok");
                    else await request.Write("Invalid code.");
                }
                break;
            case "/verify-change":
                {
                    if (!request.HasUser)
                    {
                        await request.Write("Not logged in.");
                        break;
                    }
                    if (request.User.MailToken == null)
                    {
                        await request.Write("Already verified.");
                        break;
                    }
                    if (!request.Query.TryGetValue("email", out var email))
                    {
                        request.Status = 400;
                        break;
                    }
                    string mail = email;
                    try
                    {
                        request.User.SetMailAddress(mail, request.UserTable);
                        request.User.SetNewMailToken();
                        Presets.WarningMail(request.User, "Welcome", $"Thank you for registering on <a href=\"{request.Context.ProtoHost()}\">{request.Domain}</a>.\nTo verify your email address, click <a href=\"{request.Context.ProtoHost()}{pathPrefix}/verify?code={request.User.MailToken}\">here</a> or enter the following code: {request.User.MailToken}");
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
                {
                    if (request.LoginState != LoginState.Needs2FA)
                        await request.Write("User does not need 2FA verification at the moment.");
                    else if (!request.Query.TryGetValue("code", out var code))
                        request.Status = 400;
                    else if (!request.User.TwoFactor.TOTPEnabled(out var totp))
                        request.Status = 404;
                    else if (totp.Validate(code, request, true))
                    {
                        await request.Write("ok");
                        Presets.WarningMail(request.User, "New login", "Someone just successfully logged into your account.");
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