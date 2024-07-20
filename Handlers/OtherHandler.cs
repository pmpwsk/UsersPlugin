using System.Text;
using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    private async Task Other(Request req)
    {
        switch (req.Path)
        {
            /// OVERVIEW
            case "/":
            { req.ForceGET(); req.ForceLogin();
                req.CreatePage("Account", out _, out var e);
                e.Add(new HeadingElement("Account", ""));
                if (req.IsAdmin)
                    e.Add(new ButtonElement("Manage users", null, "users"));
                e.Add(new ButtonElement("Log out", null, "logout"));
                e.Add(new ButtonElement("Log out all other devices", null, "logout-others"));
                e.Add(new ButtonElement("Settings", null, $"settings"));
            } break;



            
            /// 2FA
            case "/2fa":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        throw new RedirectSignal(req.RedirectUrl);
                    case LoginState.NeedsMailVerification:
                        throw new RedirectSignal("verify" + req.CurrentRedirectQuery);
                    case LoginState.None:
                        throw new RedirectSignal("login" + req.CurrentRedirectQuery);
                }
                req.CreatePage("2FA", out var page, out var e);
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(Presets.RedirectScript);
                page.Scripts.Add(new Script("2fa.js"));
                e.Add(new HeadingElement("Two-factor authentication"));
                e.Add(new ContainerElement(null,
                [
                    new Heading("2FA code / recovery:"),
                    new TextBox("Enter the current code...", null, "code", TextBoxRole.Username, "Continue()", autofocus: true)
                ]));
                e.Add(new ButtonElementJS("Continue", null, "Continue()", id: "continueButton"));
                Presets.AddError(page);
                e.Add(new ButtonElement(null, "Log out instead", "logout" + req.CurrentRedirectQuery));
            } break;
            
            case "/2fa/try":
            { req.ForcePOST();
                if (req.LoginState != LoginState.Needs2FA)
                    await req.Write("ok");
                else if (!req.Query.TryGetValue("code", out var code))
                    throw new BadRequestSignal();
                else if (!req.User.TwoFactor.TOTPEnabled(out var totp))
                    throw new NotFoundSignal();
                else if (totp.Validate(code, req, true))
                {
                    await req.Write("ok");
                    Presets.WarningMail(req, req.User, "New login", "Someone just successfully logged into your account.");
                }
                else await req.Write("no");
            } break;



            
            // AUTH REQUEST (LIMITED TOKEN)
            case "/auth-request":
            { req.ForceGET(); req.ForceLogin();
                req.CreatePage("Auth request", out var page, out var e);
                if (req.Query.TryGetValue("name", out var name) && name != "" && name == name.HtmlSafe() && req.Query.TryGetValue("yes", out var yes) && req.Query.TryGetValue("no", out var no) && req.Query.TryGetValue("allowed", out var limitedToPathsEncoded))
                {
                    var limitedToPaths = limitedToPathsEncoded.Split(',').Select(x => HttpUtility.UrlDecode(x).HtmlSafe()).ToList();
                    if (limitedToPaths.Contains(""))
                        throw new BadRequestSignal();
                    page.Scripts.Add(new Script("auth-request.js"));
                    string? backgroundDomain = req.Query.TryGetValue("background", out var b) && b.SplitAtFirst("://", out var bProto, out b) && (bProto == "http" || bProto == "https") && b.SplitAtFirst('/', out b, out _) ? b : null;
                    e.Add(new LargeContainerElement("Authentication request",
                    [
                        new Paragraph($"The application \"{name}\" would like to authenticate using {req.Domain}."),
                        new Paragraph("It will only get access to the following addresses:"),
                        new BulletList(limitedToPaths)
                    ]));
                    page.AddError();
                    e.Add(new ButtonElementJS("Allow", $"{(backgroundDomain == null ? "O" : $"Gives a token to \"{backgroundDomain}\" and o")}pens:<br/>{yes.Before('?').HtmlSafe()}", "Allow()", "green"));
                    e.Add(new ButtonElement("Reject", $"Opens:<br/>{no.Before('?').HtmlSafe()}", no, "red"));
                    e.Add(new ContainerElement(null,
                    [
                        "Addresses ending with * indicate that the application can access any address starting with the part before the *.",
                        "Closing this page will also reject the request, but the application won't be opened in order for it to know that it was rejected.",
                        "You can manage applications you have given access to in your account settings.",
                        "Logging out all other devices from your account menu also logs you out of any applications you have given access to.",
                        $"In order to be able to log themselves out, applications automatically get access to \"{req.PluginPathPrefix}/logout\" as well."
                    ]));
                }
                else req.Status = 400;
            } break;
                
            case "/generate-limited-token":
            { req.ForcePOST(); req.ForceLogin(false);
                if (req.Query.TryGetValue("name", out var name) && name != "" && name == name.HtmlSafe() && req.Query.TryGetValue("return", out var returnAddress) && req.Query.TryGetValue("allowed", out var limitedToPathsEncoded))
                {
                    var limitedToPaths =
                    ((IEnumerable<string>)[
                        ..limitedToPathsEncoded.Split(',').Select(x => HttpUtility.UrlDecode(x).HtmlSafe()),
                        $"{req.PluginPathPrefix}/logout"
                    ]).ToList().AsReadOnly();
                    if (limitedToPaths.Contains(""))
                        throw new BadRequestSignal();
                    string token = req.User.Auth.AddNewLimited(name, limitedToPaths);
                    AccountManager.GenerateAuthTokenCookieOptions(out var expires, out var sameSite, out var domain, req.Context);
                    await req.Write(returnAddress
                        .Replace("[TOKEN]", req.User.Id + token)
                        .Replace("[EXPIRES]", expires.Ticks.ToString())
                        .Replace("[SAMESITE]", sameSite.ToString())
                        .Replace("[DOMAIN]", domain));
                    Presets.WarningMail(req, req.User, $"App '{name}' was granted access", $"The app '{name}' has just been granted limited access to your account. You can view and manage apps with access in your account settings.");
                }
                else req.Status = 400;
            } break;




            // GET USERNAME (API)
            case "/get-username":
            { req.ForceGET();
                if (req.LoggedIn)
                    await req.Write(req.User.Username);
                else req.Status = 403;
            } break;




            /// LOGIN PAGE
            case "/login":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        throw new RedirectSignal(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        throw new RedirectSignal("2fa" + req.CurrentRedirectQuery);
                    case LoginState.NeedsMailVerification:
                        throw new RedirectSignal("verify" + req.CurrentRedirectQuery);
                }
                req.CreatePage("Login", out var page, out var e);
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
                Presets.AddError(page);
                e.Add(new ButtonElement(null, "Account recovery", $"recovery" + req.CurrentRedirectQuery));
                e.Add(new ButtonElement(null, "Register instead", $"register" + req.CurrentRedirectQuery));
            } break;

            case "/login/try":
            { req.ForcePOST();
                if (req.HasUser)
                    await req.Write("ok");
                else if (req.Query.TryGetValue("username", out var username) && req.Query.TryGetValue("password", out var password))
                {
                    User? user = req.UserTable.Login(username, password, req);
                    if (user != null)
                    {
                        user.Settings.Delete("Delete");
                        if (user.TwoFactor.TOTPEnabled())
                            await req.Write("2fa");
                        else
                        {
                            Presets.WarningMail(req, user, "New login", "Someone just successfully logged into your account.");
                            if (user.MailToken == null)
                                await req.Write("ok");
                            else await req.Write("verify");
                        }
                    }
                    else await req.Write("no");
                }
                else req.Status = 400;
            } break;




            // LOGOUT
            case "/logout":
            { req.ForceGET();
                req.UserTable.Logout(req);
                req.Redirect(req.RedirectUrl);
            } break;




            // LOGOUT OTHERS
            case "/logout-others":
            { req.ForceGET(); req.ForceLogin();
                req.CreatePage("Logout others", out var _, out var e);
                req.UserTable.LogoutOthers(req);
                e.Add(new HeadingElement("Success", "Successfully logged out all other devices and browsers."));
                e.Add(new ButtonElement("Back to account", null, "."));
            } break;




            // REGISTRATION PAGE
            case "/register":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        throw new RedirectSignal(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        throw new RedirectSignal("2fa" + req.CurrentRedirectQuery);
                    case LoginState.NeedsMailVerification:
                        throw new RedirectSignal("verify" + req.CurrentRedirectQuery);
                }
                req.CreatePage("Register", out var page, out var e);
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
                Presets.AddError(page);
                e.Add(new ButtonElement(null, "Log in instead", "login" + req.CurrentRedirectQuery));
            } break;

            case "/register/try":
            { req.ForcePOST();
                if (req.HasUser)
                    await req.Write("ok");
                else if (req.Query.TryGetValue("username", out var username) && req.Query.TryGetValue("email", out var email) && req.Query.TryGetValue("password", out var password))
                {
                    try
                    {
                        User user = req.UserTable.Register(username, email, password, req);
                        Presets.WarningMail(req, user, "Welcome", $"Thank you for registering on <a href=\"{req.Context.ProtoHost()}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.PluginPathPrefix}/verify?user={user.Id}&code={user.MailToken}\">here</a> or enter the following code: {user.MailToken}");
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
            } break;




            /// USER STYLE
            case "/theme.css":
            { req.ForceGET();
                ThemeFromQuery(req.Context.Request.QueryString.Value??"", out string font, out string? fontMono, out string background, out string accent, out string design);
                string domain = req.Domain;
                string timestamp = Timestamp(font, fontMono, background, accent, design);
                if (Server.Config.MimeTypes.TryGetValue(".css", out string? type)) req.Context.Response.ContentType = type;
                if (Server.Config.FileCorsDomain != null)
                    req.CorsDomain = Server.Config.FileCorsDomain;
                if (Server.Config.BrowserCacheMaxAge.TryGetValue(".css", out int maxAge))
                {
                    if (maxAge == 0)
                        req.Context.Response.Headers.CacheControl = "no-cache, private";
                    else
                    {
                        req.Context.Response.Headers.CacheControl = "public, max-age=" + maxAge;
                        try
                        {
                            if (req.Context.Request.Headers.TryGetValue("If-None-Match", out var oldTag) && oldTag == timestamp)
                            {
                                req.Context.Response.StatusCode = 304;
                                break; //browser already has the current version
                            }
                            else req.Context.Response.Headers.ETag = timestamp;
                        }
                        catch { }
                    }
                }
                await req.WriteBytes(
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
                    ..Encoding.UTF8.GetBytes($"\n\n\n/* Design: {design} */\n\n"),
                    ..GetFile($"/theme/d/{design}.css", req.PluginPathPrefix, domain) ?? []
                ]);
            } break;




            // VERIFY EMAIL ADDRESS
            case "/verify":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        throw new RedirectSignal(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        throw new RedirectSignal("2fa" + req.CurrentRedirectQuery);
                }
                req.CreatePage("Verify", out var page, out var e);
                if (req.Query.TryGetValue("user", out var uid) && req.Query.TryGetValue("code", out var code))
                {
                    User user;
                    if (req.HasUser && req.User.Id == uid)
                        user = req.User;
                    else if (req.UserTable.TryGetValue(uid, out var u))
                        user = u;
                    else goto SKIP_QUERY;
                    
                    if (user.MailToken == null)
                        req.Redirect(req.RedirectUrl);
                    else if (user.VerifyMail(code, req))
                        e.Add(new HeadingElement("Verified!", "You have successfully verified your email address.", "green"));
                    else goto SKIP_QUERY;
                    
                    break;
                }
            SKIP_QUERY:
                if (req.LoginState == LoginState.None)
                    throw new RedirectSignal("login" + req.CurrentRedirectQuery);
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
                Presets.AddError(page);
                e.Add(new ButtonElement(null, "Change email address", "verify/change"));
                e.Add(new ButtonElement(null, "Log out instead", "logout" + req.CurrentRedirectQuery));
            } break;
            
            case "/verify/try":
            { req.ForcePOST();
                if (!req.HasUser)
                    throw new ForbiddenSignal();
                else if (req.User.MailToken == null)
                    await req.Write("ok");
                else if (!req.Query.TryGetValue("code", out var code))
                    throw new BadRequestSignal();
                else if (req.User.VerifyMail(code, req))
                    await req.Write("ok");
                else await req.Write("no");
            } break;
            
            case "/verify/resend":
            { req.ForcePOST();
                if (!req.HasUser)
                    throw new ForbiddenSignal();
                else if (req.User.MailToken == null)
                    break;
                Presets.WarningMail(req, req.User, "Welcome", $"Thank you for registering on <a href=\"{req.Context.ProtoHost()}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.PluginPathPrefix}/verify?user={req.User.Id}&code={req.User.MailToken}\">here</a> or enter the following code: {req.User.MailToken}");
            } break;




            // CHANGE EMAIL DURING VERIFICATION
            case "/verify/change":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        throw new RedirectSignal(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        throw new RedirectSignal("../2fa" + req.CurrentRedirectQuery);
                    case LoginState.None:
                        throw new RedirectSignal("../login" + req.CurrentRedirectQuery);
                }
                req.CreatePage("Verify", out var page, out var e);
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
                Presets.AddError(page);
                e.Add(new ButtonElement(null, "Back", "../verify" + req.CurrentRedirectQuery));
            } break;

            case "/verify/set-email":
            {  req.ForcePOST();
                if (!req.HasUser)
                    throw new ForbiddenSignal();
                else if (req.User.MailToken == null)
                    await req.Write("ok");
                else if (req.Query.TryGetValue("email", out var mail))
                {
                    try
                    {
                        req.User.SetMailAddress(mail, req.UserTable);
                        req.User.SetNewMailToken();
                        Presets.WarningMail(req, req.User, "Welcome", $"Thank you for registering on <a href=\"{req.Context.ProtoHost()}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.PluginPathPrefix}/verify?user={req.User.Id}&code={req.User.MailToken}\">here</a> or enter the following code: {req.User.MailToken}");
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
            } break;




            // 404
            default:
                req.CreatePage("Error");
                req.Status = 404;
                break;
        }
    }
}