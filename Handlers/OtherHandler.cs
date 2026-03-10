using System.Text;
using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

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
                var page = new Page(req, false);
                page.Title = "Account";
                var subsection = new Subsection(null, null);
                page.Sections.Add(new Section("Account", [ subsection ]));
                if (req.IsAdmin)
                    subsection.Content.Add(new BigLinkButton(new("bi bi-people", "Manage users"), [ "Control all existing users." ], "users"));
                subsection.Content.Add(new BigLinkButton(new("bi bi-box-arrow-left", "Log out"), [ "Other devices will stay logged in." ], "logout"));
                subsection.Content.Add(new BigLinkButton(new("bi bi-x-circle", "Log out all other devices"), [ "This device will stay logged in." ], "logout-others"));
                subsection.Content.Add(new BigLinkButton(new("bi bi-gear", "Settings"), [ "Control your account details." ], "settings"));
                return page;
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
                page.Scripts.Add(new Elements.Script("2fa.js"));
                e.Add(new Elements.HeadingElement("Two-factor authentication"));
                e.Add(new Elements.ContainerElement(null,
                [
                    new Elements.Heading("2FA code / recovery:"),
                    new Elements.TextBox("Enter the current code...", null, "code", Elements.TextBoxRole.NoSpellcheck, "Continue()", autofocus: true)
                ]));
                e.Add(new Elements.ButtonElementJS("Continue", null, "Continue()", id: "continueButton"));
                page.AddError();
                e.Add(new Elements.ButtonElement(null, "Log out instead", "logout" + req.CurrentRedirectQuery));
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
                    page.Scripts.Add(new Elements.Script("auth-request.js"));
                    string? backgroundDomain = req.Query.TryGetValue("background", out var b) && b.SplitAtFirst("://", out var bProto, out b) && (bProto == "http" || bProto == "https") && b.SplitAtFirst('/', out b, out _) ? b : null;
                    e.Add(new Elements.LargeContainerElement("Authentication request",
                    [
                        new Elements.Paragraph($"The application \"{name}\" would like to authenticate using {req.Domain}."),
                        new Elements.Paragraph("It will only get access to the following addresses:"),
                        new Elements.BulletList(limitedToPaths)
                    ]));
                    page.AddError();
                    e.Add(new Elements.ButtonElementJS("Allow", $"{(backgroundDomain == null ? "O" : $"Gives a token to \"{backgroundDomain.HtmlSafe()}\" and o")}pens:<br/>{yes.Before('?').HtmlSafe()}", "Allow()", "green") {Unsafe = true});
                    e.Add(new Elements.ButtonElement("Reject", $"Opens:<br/>{no.Before('?').HtmlSafe()}", no.HtmlSafe(), "red") {Unsafe = true});
                    e.Add(new Elements.ContainerElement(null,
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
                var page = new Page(req, true);
                page.Title = "Login";
                var usernameBox = new TextBox("username", "Enter your username...", null, TextBoxRole.Username);
                var passwordBox = new TextBox("password", "Enter your password...", null, TextBoxRole.CurrentPassword);
                var submitButton = new SubmitButton("Continue");
                page.Sections.Add(new(
                    "Login",
                    [
                        new ServerForm(
                            null,
                            actionReq => TryLogin(req, actionReq, page, usernameBox.Value, passwordBox.Value),
                            [
                                new Heading3("Username"),
                                usernameBox,
                                new Heading3("Password"),
                                passwordBox,
                                submitButton
                            ]
                        ),
                        new Subsection(
                            null,
                            [
                                new BigLinkButton("Account recovery", ["Can't access your account?"], "recovery" + req.CurrentRedirectQuery),
                                new BigLinkButton("Register instead", ["Don't have an account yet?"], "register" + req.CurrentRedirectQuery)
                            ]
                        )
                    ]
                ));
                return page;
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
                var page = new Page(req, false);
                page.Title = "Logout others";
                await req.UserTable.LogoutOthersAsync(req);
                page.Sections.Add(new Section(
                    "Success",
                    [
                        new Subsection(
                            null,
                            [
                                new Paragraph("Successfully logged out all other devices and browsers."),
                                new LinkButton("Back to account", ".")
                            ]
                        )
                    ]
                ));
                return page;
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
                page.Scripts.Add(new Elements.Script("register.js"));
                e.Add(new Elements.HeadingElement("Register"));
                e.Add(new Elements.ContainerElement(null,
                [
                    new Elements.Heading("Username:"),
                    new Elements.TextBox("Enter a username...", null, "username", Elements.TextBoxRole.Username, "Continue()", autofocus: true),
                    new Elements.Heading("Email:"),
                    new Elements.TextBox("Enter your email address...", null, "email", Elements.TextBoxRole.Email, "Continue()"),
                    new Elements.Heading("Password:"),
                    new Elements.TextBox("Enter a password...", null, "password1", Elements.TextBoxRole.NewPassword, "Continue()"),
                    new Elements.Heading("Confirm password:"),
                    new Elements.TextBox("Enter the password again...", null, "password2", Elements.TextBoxRole.NewPassword, "Continue()")
                ]));
                e.Add(new Elements.ButtonElementJS("Continue", null, "Continue()", id: "continueButton"));
                page.AddError();
                e.Add(new Elements.ButtonElement(null, "Log in instead", "login" + req.CurrentRedirectQuery));
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
                    e.Add(new Elements.HeadingElement("Verified!", "You have successfully verified your email address.", "green"));
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
                page.Scripts.Add(new Elements.Script("verify.js"));
                e.Add(new Elements.HeadingElement("Mail verification"));
                e.Add(new Elements.ContainerElement(null,
                [
                    new Elements.Heading("Verification code:"),
                    new Elements.TextBox("Enter the code...", null, "code", onEnter: "Continue()", autofocus: true)
                ]) { Buttons = [ new Elements.ButtonJS("Send again", "Resend()") ] });
                e.Add(new Elements.ButtonElementJS("Continue", null, "Continue()"));
                page.AddError();
                e.Add(new Elements.ButtonElement(null, "Change email address", "verify/change"));
                e.Add(new Elements.ButtonElement(null, "Log out instead", "logout" + req.CurrentRedirectQuery));
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
                page.Scripts.Add(new Elements.Script("change.js"));
                e.Add(new Elements.HeadingElement("Change email address"));
                e.Add(new Elements.ContainerElement(null,
                [
                    new Elements.Heading("Email:"),
                    new Elements.TextBox("Enter your email address...", req.User.MailAddress, "email", onEnter: "Continue()", autofocus: true)
                ]));
                e.Add(new Elements.ButtonElementJS("Change", null, "Continue()"));
                page.AddError();
                e.Add(new Elements.ButtonElement(null, "Back", "../verify" + req.CurrentRedirectQuery));
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
    
    private static async Task<IActionResponse> TryLogin(Request pageReq, Request actionReq, Page page, string username, string password)
    {
        if (actionReq.HasUser)
            return new Navigate(pageReq.RedirectUrl);
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return page.DynamicErrorAction("Please enter your username and password.");

        User? user = await actionReq.UserTable.LoginAsync(username, password, actionReq);
        if (user != null)
        {
            if (user.Settings.ContainsKey("Delete"))
                await actionReq.UserTable.DeleteSettingAsync(user.Id, "Delete");
            if (user.TwoFactor.TOTPEnabled())
                return new Navigate("2fa" + pageReq.CurrentRedirectQuery);
            else
            {
                await Presets.WarningMailAsync(actionReq, user, "New login", "Someone just successfully logged into your account.");
                if (user.MailToken == null)
                    return new Navigate(pageReq.RedirectUrl);
                else
                    return new Navigate("verify" + pageReq.CurrentRedirectQuery);
            }
        }
        else
            return page.DynamicErrorAction("The combination of username and password you have entered isn't correct.");
    }
}