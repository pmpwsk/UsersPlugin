using System.Text;
using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;
using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private async Task<IResponse> HandleOther(Request req)
    {
        switch (req.Path)
        {
            // OVERVIEW
            case "/":
            { req.ForceGET(); req.ForceLogin();
                Presets.CreatePage(req, "Account", out var page, out var e);
                e.Add(new HeadingElement("Account", ""));
                if (req.IsAdmin)
                    e.Add(new ButtonElement("Manage users", null, "users"));
                e.Add(new ButtonElement("Log out", null, "logout"));
                e.Add(new ButtonElement("Log out all other devices", null, "logout-others"));
                e.Add(new ButtonElement("Settings", null, $"settings"));
                return new LegacyPageResponse(page, req);
            }



            
            // 2FA
            case "/2fa":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        return new RedirectResponse(req.RedirectUrl);
                    case LoginState.NeedsMailVerification:
                        return new RedirectResponse("verify" + req.CurrentRedirectQuery);
                    case LoginState.None:
                        return new RedirectResponse("login" + req.CurrentRedirectQuery);
                }
                Presets.CreatePage(req, "2FA", out var page, out var e);
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(Presets.RedirectScript);
                page.Scripts.Add(new Script("2fa.js"));
                e.Add(new HeadingElement("Two-factor authentication"));
                e.Add(new ContainerElement(null,
                [
                    new Heading("2FA code / recovery:"),
                    new TextBox("Enter the current code...", null, "code", TextBoxRole.NoSpellcheck, "Continue()", autofocus: true)
                ]));
                e.Add(new ButtonElementJS("Continue", null, "Continue()", id: "continueButton"));
                page.AddError();
                e.Add(new ButtonElement(null, "Log out instead", "logout" + req.CurrentRedirectQuery));
                return new LegacyPageResponse(page, req);
            }
            
            case "/2fa/try":
            { req.ForcePOST();
                if (req.LoginState != LoginState.Needs2FA)
                    return new TextResponse("ok");
                var code = req.Query.GetOrThrow("code");
                if (!req.User.TwoFactor.TOTPEnabled())
                    return StatusResponse.NotFound;
                else if (await req.UserTable.ValidateTOTPAsync(req.User.Id, code, req, true))
                {
                    await Presets.WarningMailAsync(req, req.User, "New login", "Someone just successfully logged into your account.");
                    return new TextResponse("ok");
                }
                else return new TextResponse("no");
            }



            
            // AUTH REQUEST (LIMITED TOKEN)
            case "/auth-request":
            { req.ForceGET(); req.ForceLogin();
                Presets.CreatePage(req, "Auth request", out var page, out var e);
                if (req.Query.TryGetValue("name", out var name) && name != "" && name == name.HtmlSafe() && req.Query.TryGetValue("yes", out var yes) && req.Query.TryGetValue("no", out var no) && req.Query.TryGetValue("allowed", out var limitedToPathsEncoded))
                {
                    var limitedToPaths = limitedToPathsEncoded.Split(',').Select(x => HttpUtility.UrlDecode(x).HtmlSafe()).ToList();
                    if (limitedToPaths.Contains(""))
                        return StatusResponse.BadRequest;
                    page.Scripts.Add(new Script("auth-request.js"));
                    string? backgroundDomain = req.Query.TryGetValue("background", out var b) && b.SplitAtFirst("://", out var bProto, out b) && (bProto == "http" || bProto == "https") && b.SplitAtFirst('/', out b, out _) ? b : null;
                    e.Add(new LargeContainerElement("Authentication request",
                    [
                        new Paragraph($"The application \"{name}\" would like to authenticate using {req.Domain}."),
                        new Paragraph("It will only get access to the following addresses:"),
                        new BulletList(limitedToPaths)
                    ]));
                    page.AddError();
                    e.Add(new ButtonElementJS("Allow", $"{(backgroundDomain == null ? "O" : $"Gives a token to \"{backgroundDomain.HtmlSafe()}\" and o")}pens:<br/>{yes.Before('?').HtmlSafe()}", "Allow()", "green") {Unsafe = true});
                    e.Add(new ButtonElement("Reject", $"Opens:<br/>{no.Before('?').HtmlSafe()}", no.HtmlSafe(), "red") {Unsafe = true});
                    e.Add(new ContainerElement(null,
                    [
                        "Addresses ending with * indicate that the application can access any address starting with the part before the *.",
                        "Closing this page will also reject the request, but the application won't be opened in order for it to know that it was rejected.",
                        "You can manage applications you have given access to in your account settings.",
                        "Logging out all other devices from your account menu also logs you out of any applications you have given access to.",
                        $"In order to be able to log themselves out, applications automatically get access to \"{req.PluginPathPrefix}/logout\" as well."
                    ]));
                    return new LegacyPageResponse(page, req);
                }
                else
                    return StatusResponse.BadRequest;
            }
                
            case "/auth-request/generate-limited-token":
            { req.ForcePOST(); req.ForceLogin(false);
                if (req.Query.TryGetValue("name", out var name) && name != "" && name == name.HtmlSafe() && req.Query.TryGetValue("return", out var returnAddress) && req.Query.TryGetValue("allowed", out var limitedToPathsEncoded))
                {
                    var limitedToPaths =
                    ((IEnumerable<string>)[
                        ..limitedToPathsEncoded.Split(',').Select(x => HttpUtility.UrlDecode(x).HtmlSafe()),
                        $"{req.PluginPathPrefix}/logout"
                    ]).ToList().AsReadOnly();
                    if (limitedToPaths.Contains(""))
                        return StatusResponse.BadRequest;
                    string token = await req.UserTable.AddNewLimitedTokenAsync(req.User.Id, name, limitedToPaths);
                    AccountManager.GenerateAuthTokenCookieOptions(out var expires, out var sameSite, out var domain, req);
                    await Presets.WarningMailAsync(req, req.User, $"App '{name}' was granted access", $"The app '{name}' has just been granted limited access to your account. You can view and manage apps with access in your account settings.");
                    return new TextResponse(returnAddress
                        .Replace("[TOKEN]", req.User.Id + token)
                        .Replace("[EXPIRES]", expires.Ticks.ToString())
                        .Replace("[SAMESITE]", sameSite.ToString())
                        .Replace("[DOMAIN]", domain));
                }
                else
                    return StatusResponse.BadRequest;
            }




            // GET USERNAME (API)
            case "/get-username":
            { req.ForceGET();
                if (req.LoggedIn)
                    return new TextResponse(req.User.Username);
                else
                    return StatusResponse.Forbidden;
            }




            // LOGIN PAGE
            case "/login":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        return new RedirectResponse(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        return new RedirectResponse("2fa" + req.CurrentRedirectQuery);
                    case LoginState.NeedsMailVerification:
                        return new RedirectResponse("verify" + req.CurrentRedirectQuery);
                }
                Presets.CreatePage(req, "Login", out var page, out var e);
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(Presets.RedirectScript);
                page.Scripts.Add(Presets.RedirectQueryScript);
                page.Scripts.Add(new Script("login.js"));
                e.Add(new HeadingElement("Login"));
                e.Add(new ContainerElement(null,
                [
                    new Heading("Username:"),
                    new TextBox("Enter your username...", null, "username", TextBoxRole.Username, "Continue()", autofocus: true),
                    new Heading("Password:"),
                    new TextBox("Enter your password...", null, "password", TextBoxRole.Password, "Continue()"),
                ]));
                e.Add(new ButtonElementJS("Continue", null, "Continue()", id: "continueButton"));
                page.AddError();
                e.Add(new ButtonElement(null, "Account recovery", $"recovery" + req.CurrentRedirectQuery));
                e.Add(new ButtonElement(null, "Register instead", $"register" + req.CurrentRedirectQuery));
                return new LegacyPageResponse(page, req);
            }

            case "/login/try":
            { req.ForcePOST();
                if (req.HasUser)
                    return new TextResponse("ok");
                var username = req.Query.GetOrThrow("username");
                var password = req.Query.GetOrThrow("password");
                
                User? user = await req.UserTable.LoginAsync(username, password, req);
                if (user != null)
                {
                    if (user.Settings.ContainsKey("Delete"))
                        await req.UserTable.DeleteSettingAsync(user.Id, "Delete");
                    if (user.TwoFactor.TOTPEnabled())
                        return new TextResponse("2fa");
                    else
                    {
                        await Presets.WarningMailAsync(req, user, "New login", "Someone just successfully logged into your account.");
                        if (user.MailToken == null)
                            return new TextResponse("ok");
                        else
                            return new TextResponse("verify");
                    }
                }
                else
                    return new TextResponse("no");
            }




            // LOGOUT
            case "/logout":
            { req.ForceGET();
                await req.UserTable.LogoutAsync(req);
                return new RedirectResponse(req.RedirectUrl);
            }




            // LOGOUT OTHERS
            case "/logout-others":
            { req.ForceGET(); req.ForceLogin();
                Presets.CreatePage(req, "Logout others", out var page, out var e);
                await req.UserTable.LogoutOthersAsync(req);
                e.Add(new HeadingElement("Success", "Successfully logged out all other devices and browsers."));
                e.Add(new ButtonElement("Back to account", null, "."));
                return new LegacyPageResponse(page, req);
            }




            // REGISTRATION PAGE
            case "/register":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        return new RedirectResponse(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        return new RedirectResponse("2fa" + req.CurrentRedirectQuery);
                    case LoginState.NeedsMailVerification:
                        return new RedirectResponse("verify" + req.CurrentRedirectQuery);
                }
                Presets.CreatePage(req, "Register", out var page, out var e);
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(Presets.RedirectQueryScript);
                page.Scripts.Add(new Script("register.js"));
                e.Add(new HeadingElement("Register"));
                e.Add(new ContainerElement(null,
                [
                    new Heading("Username:"),
                    new TextBox("Enter a username...", null, "username", TextBoxRole.Username, "Continue()", autofocus: true),
                    new Heading("Email:"),
                    new TextBox("Enter your email address...", null, "email", TextBoxRole.Email, "Continue()"),
                    new Heading("Password:"),
                    new TextBox("Enter a password...", null, "password1", TextBoxRole.NewPassword, "Continue()"),
                    new Heading("Confirm password:"),
                    new TextBox("Enter the password again...", null, "password2", TextBoxRole.NewPassword, "Continue()")
                ]));
                e.Add(new ButtonElementJS("Continue", null, "Continue()", id: "continueButton"));
                page.AddError();
                e.Add(new ButtonElement(null, "Log in instead", "login" + req.CurrentRedirectQuery));
                return new  LegacyPageResponse(page, req);
            }

            case "/register/try":
            { req.ForcePOST();
                if (req.HasUser)
                    return new TextResponse("ok");
                var username = req.Query.GetOrThrow("username");
                var email = req.Query.GetOrThrow("email");
                var password = req.Query.GetOrThrow("password");
                try
                {
                    User user = await req.UserTable.RegisterAsync(username, email, password, req);
                    await Presets.WarningMailAsync(req, user, "Welcome", $"Thank you for registering on <a href=\"{req.ProtoHost}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.PluginPathPrefix}/verify?user={user.Id}&code={user.MailToken}\">here</a> or enter the following code: {user.MailToken}");
                    return new TextResponse("ok");
                }
                catch (Exception ex)
                {
                    return new TextResponse(ex.Message switch
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




            // USER STYLE
            case "/theme.css":
            { req.ForceGET();
                ThemeFromQuery(req.Query.FullString, out string font, out string? fontMono, out string background, out string accent, out string design);
                string domain = req.Domain;
                string timestamp = Timestamp(font, fontMono, background, accent, design);
                return new ByteArrayResponse(
                [
                    ..Encoding.UTF8.GetBytes($"/* Font declaration: {font} */\n\n"),
                    ..GetFile($"/theme/f/{font}.css", req.PluginPathPrefix, domain) ?? [],
                    ..(fontMono == null ? (byte[])[] :
                    [
                        ..Encoding.UTF8.GetBytes($"\n\n\n/* Font declaration: {fontMono} */\n\n"),
                        ..GetFile($"/theme/f/{fontMono}.css", req.PluginPathPrefix, domain) ?? [],
                    ]),
                    ..Encoding.UTF8.GetBytes($"\n\n\n/* Font: {font} */\n\n:root {{\n\t--font-family: '{font}';\n\t--font-code: '{fontMono??font}';\n}}\n"),
                    ..Encoding.UTF8.GetBytes($"\n\n\n/* Accent: {AccentName(accent, background)} */\n\n"),
                    ..GetFile($"/theme/a/{AccentName(accent, background)}.css", req.PluginPathPrefix, domain) ?? [],
                    ..Encoding.UTF8.GetBytes($"\n\n\n/* Background: {background} */\n\n"),
                    ..GetFile($"/theme/b/{background}.css", req.PluginPathPrefix, domain) ?? [],
                    ..Encoding.UTF8.GetBytes($"\n\n\n/* base.css */\n\n"),
                    ..GetFile($"/theme/base.css", req.PluginPathPrefix, domain) ?? [],
                    ..Encoding.UTF8.GetBytes($"\n\n\n/* Design: {design} */\n\n"),
                    ..GetFile($"/theme/d/{design}.css", req.PluginPathPrefix, domain) ?? []
                ], ".css", true, timestamp);
            }




            // VERIFY EMAIL ADDRESS
            case "/verify":
            { req.ForceGET();
                Presets.CreatePage(req, "Verify", out var page, out var e);
                var uid = req.Query.GetOrThrow("user");
                var code = req.Query.GetOrThrow("code");
                
                User user;
                if (req.HasUser && req.User.Id == uid)
                    user = req.User;
                else
                {
                    var u = await req.UserTable.GetByIdNullableAsync(uid);
                    if (u != null)
                        user = u;
                    else goto SKIP_QUERY;
                }
                
                if (user.MailToken == null)
                    return new RedirectResponse(req.RedirectUrl);
                else if (await req.UserTable.VerifyMailAsync(user.Id, code, req))
                {
                    e.Add(new HeadingElement("Verified!", "You have successfully verified your email address.", "green"));
                    return new LegacyPageResponse(page, req);
                }
                SKIP_QUERY:
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        return new RedirectResponse(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        return new RedirectResponse("2fa" + req.CurrentRedirectQuery);
                    case LoginState.None:
                        return new RedirectResponse("login" + req.CurrentRedirectQuery);
                }
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(Presets.RedirectScript);
                page.Scripts.Add(new Script("verify.js"));
                e.Add(new HeadingElement("Mail verification"));
                e.Add(new ContainerElement(null,
                [
                    new Heading("Verification code:"),
                    new TextBox("Enter the code...", null, "code", onEnter: "Continue()", autofocus: true)
                ]) { Buttons = [ new ButtonJS("Send again", "Resend()") ] });
                e.Add(new ButtonElementJS("Continue", null, "Continue()"));
                page.AddError();
                e.Add(new ButtonElement(null, "Change email address", "verify/change"));
                e.Add(new ButtonElement(null, "Log out instead", "logout" + req.CurrentRedirectQuery));
                return new LegacyPageResponse(page, req);
            }
            
            case "/verify/try":
            { req.ForcePOST();
                if (!req.HasUser)
                    return StatusResponse.Forbidden;
                if (req.User.MailToken == null)
                    return new TextResponse("ok");
                var code = req.Query.GetOrThrow("code");
                if (await req.UserTable.VerifyMailAsync(req.User.Id, code, req))
                    return new TextResponse("ok");
                return new TextResponse("no");
            }
            
            case "/verify/resend":
            { req.ForcePOST();
                if (!req.HasUser)
                    return StatusResponse.Forbidden;
                else if (req.User.MailToken == null)
                    return StatusResponse.Success;
                await Presets.WarningMailAsync(req, req.User, "Welcome", $"Thank you for registering on <a href=\"{req.ProtoHost}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.PluginPathPrefix}/verify?user={req.User.Id}&code={req.User.MailToken}\">here</a> or enter the following code: {req.User.MailToken}");
                return StatusResponse.Success;
            }




            // CHANGE EMAIL DURING VERIFICATION
            case "/verify/change":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        return new RedirectResponse(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        return new RedirectResponse("../2fa" + req.CurrentRedirectQuery);
                    case LoginState.None:
                        return new RedirectResponse("../login" + req.CurrentRedirectQuery);
                }
                Presets.CreatePage(req, "Verify", out var page, out var e);
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(Presets.RedirectScript);
                page.Scripts.Add(new Script("change.js"));
                e.Add(new HeadingElement("Change email address"));
                e.Add(new ContainerElement(null,
                [
                    new Heading("Email:"),
                    new TextBox("Enter your email address...", req.User.MailAddress, "email", onEnter: "Continue()", autofocus: true)
                ]));
                e.Add(new ButtonElementJS("Change", null, "Continue()"));
                page.AddError();
                e.Add(new ButtonElement(null, "Back", "../verify" + req.CurrentRedirectQuery));
                return new LegacyPageResponse(page, req);
            }

            case "/verify/set-email":
            {  req.ForcePOST();
                if (!req.HasUser)
                    return StatusResponse.Forbidden;
                if (req.User.MailToken == null)
                    return new TextResponse("ok");
                var mail = req.Query.GetOrThrow("email");
                try
                {
                    await req.UserTable.SetMailAddressAsync(req.User.Id, mail);
                    await req.UserTable.SetNewMailTokenAsync(req.User.Id);
                    await Presets.WarningMailAsync(req, req.User, "Welcome", $"Thank you for registering on <a href=\"{req.ProtoHost}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.PluginPathPrefix}/verify?user={req.User.Id}&code={req.User.MailToken}\">here</a> or enter the following code: {req.User.MailToken}");
                    return new TextResponse("ok");
                }
                catch (Exception ex)
                {
                    return new TextResponse(ex.Message switch
                    {
                        "Another user with the provided mail address already exists." => "exists",
                        "The provided mail address is the same as the old one." => "same",
                        "Invalid mail address format." => "bad",
                        _ => "error"
                    });
                }
            }




            // 404
            default:
                return StatusResponse.NotFound;
        }
    }
}