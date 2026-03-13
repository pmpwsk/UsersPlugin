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
                var subsection = new Subsection(null);
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
                var usernameInput = new TextBox("username", "Enter your username...", null, TextBoxRole.Username) { Autofocus = true };
                var passwordInput = new TextBox("password", "Enter your password...", null, TextBoxRole.CurrentPassword);
                page.Sections.Add(new(
                    "Login",
                    [
                        new ServerForm(
                            null,
                            async actionReq =>
                            {
                                if (actionReq.HasUser)
                                    return new Navigate(req.RedirectUrl);
                                if (usernameInput.IsEmpty || passwordInput.IsEmpty)
                                    return page.DynamicErrorAction("Please enter your username and password.");

                                User? user = await actionReq.UserTable.LoginAsync(usernameInput.Value, passwordInput.Value, actionReq);
                                if (user != null)
                                {
                                    if (user.Settings.ContainsKey("Delete"))
                                        await actionReq.UserTable.DeleteSettingAsync(user.Id, "Delete");
                                    if (user.TwoFactor.TOTPEnabled())
                                        return new Navigate("2fa" + req.CurrentRedirectQuery);
                                    else
                                    {
                                        await Presets.WarningMailAsync(actionReq, user, "New login", "Someone just successfully logged into your account.");
                                        if (user.MailToken == null)
                                            return new Navigate(req.RedirectUrl);
                                        else
                                            return new Navigate("verify" + req.CurrentRedirectQuery);
                                    }
                                }
                                else
                                    return page.DynamicErrorAction("The combination of username and password you have entered isn't correct.");
                            },
                            [
                                new Heading3("Username"),
                                usernameInput,
                                new Heading3("Password"),
                                passwordInput,
                                new SubmitButton(new("bi bi-arrow-return-right", "Continue"))
                            ]
                        ),
                        new Subsection(
                            null,
                            [
                                new BigLinkButton(new("bi bi-life-preserver", "Account recovery"), ["Can't access your account?"], "recovery" + req.CurrentRedirectQuery),
                                new BigLinkButton(new ("bi bi-person-vcard", "Register instead"), ["Don't have an account yet?"], "register" + req.CurrentRedirectQuery)
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
                var page = new Page(req, true);
                page.Title = "Register";
                var usernameInput = new TextBox("username", "Enter a username...", null, TextBoxRole.Username) { Autofocus = true };
                var emailInput = new TextBox("email", "Enter your email address...", null, TextBoxRole.Email);
                var passwordInput1 = new TextBox("password1", "Enter a password...", null, TextBoxRole.NewPassword);
                var passwordInput2 = new TextBox("password2", "Enter the password again...", null, TextBoxRole.NewPassword);
                page.Sections.Add(new(
                    "Register",
                    [
                        new ServerForm(
                            null,
                            async actionReq =>
                            {
                                if (!actionReq.HasUser)
                                {
                                    if (usernameInput.IsEmpty || emailInput.IsEmpty || passwordInput1.IsEmpty || passwordInput2.IsEmpty)
                                        return page.DynamicErrorAction("Please fill out all fields.");
                                    if (passwordInput1.Value != passwordInput2.Value)
                                        return page.DynamicErrorAction("The passwords do not match.");
                                    
                                    try
                                    {
                                        User user = await req.UserTable.RegisterAsync(usernameInput.Value, emailInput.Value, passwordInput1.Value, actionReq);
                                        await Presets.WarningMailAsync(req, user, "Welcome", $"Thank you for registering on <a href=\"{req.ProtoHost}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.PluginPathPrefix}/verify-link?user={user.Id}&code={user.MailToken}\">here</a> or enter the following code: {user.MailToken}");
                                    }
                                    catch (Exception ex)
                                    {
                                        return page.DynamicErrorAction(ex.Message);
                                    }
                                }
                                    
                                return new Navigate("verify" + req.CurrentRedirectQuery);
                            },
                            [
                                new Heading3("Username"),
                                usernameInput,
                                new Heading3("Email"),
                                emailInput,
                                new Heading3("Password"),
                                passwordInput1,
                                new Heading3("Confirm password"),
                                passwordInput2,
                                new SubmitButton(new("bi bi-arrow-return-right", "Continue"))
                            ]
                        ),
                        new Subsection(
                            null,
                            [
                                new BigLinkButton(new("bi bi-key", "Log in instead"), ["Already have an account?"], "login" + req.CurrentRedirectQuery)
                            ]
                        )
                    ]
                ));
                return page;
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
                if (!req.HasUser)
                    return StatusResponse.NotAuthenticated;
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        return new RedirectResponse(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        return new RedirectResponse("2fa" + req.CurrentRedirectQuery);
                    case LoginState.None:
                        return new RedirectResponse("login" + req.CurrentRedirectQuery);
                    case LoginState.NeedsMailVerification:
                        break;
                    default:
                        return StatusResponse.Forbidden;
                }
                var page = new Page(req, true);
                page.Title = "Verify";
                var codeInput = new TextBox("code", "Enter the code...", null, TextBoxRole.NoSpellcheck);
                page.Sections.Add(new(
                    "Verify",
                    [
                        new Subsection(
                            null,
                            [
                                new Paragraph("You should have received an email containing a verification code, please enter it below to finish setting up your account."),
                                new ServerActionButton(new("bi bi-envelope", "Send again"), async actionReq =>
                                {
                                    if (actionReq.HasUser && actionReq.User.MailToken != null)
                                        await Presets.WarningMailAsync(req, actionReq.User, "Welcome", $"Thank you for registering on <a href=\"{req.ProtoHost}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.PluginPathPrefix}/verify-link?user={req.User.Id}&code={req.User.MailToken}\">here</a> or enter the following code: {req.User.MailToken}");
                                    
                                    return page.DynamicInfoAction("The code has been sent.");
                                })
                            ]
                        ),
                        new ServerForm(
                            null,
                            async actionReq =>
                            {
                                if (codeInput.IsEmpty)
                                    return page.DynamicErrorAction("Please enter the verification code.");
                                
                                if (!actionReq.HasUser || actionReq.User.MailToken == null
                                    || await req.UserTable.VerifyMailAsync(actionReq.User.Id, codeInput.Value, actionReq))
                                    return new Navigate(req.RedirectUrl);
                                else
                                    return page.DynamicErrorAction("The provided code is invalid.");
                            },
                            [
                                new Heading3("Verification code"),
                                codeInput,
                                new SubmitButton(new("bi bi-arrow-return-right", "Verify"))
                            ]
                        ),
                        new Subsection(
                            null,
                            [
                                new BigLinkButton(new("bi bi-arrow-left-right", "Change email address"), ["Try another email address."], "verify-change"),
                                new BigLinkButton(new("bi bi-box-arrow-left", "Log out instead"), ["Set up your account later."], $"logout{req.CurrentRedirectQuery}")
                            ]
                        )
                    ]
                ));
                return page;
            }
            
            case "/verify-link":
            { req.ForceGET();
                var uid = req.Query.GetOrThrow("user");
                var code = req.Query.GetOrThrow("code");
                var user = await req.UserTable.GetByIdAsync(uid);
                if (user.MailToken == null)
                    return new RedirectResponse(req.RedirectUrl);
                var page = new Page(req, false);
                if (await req.UserTable.VerifyMailAsync(user.Id, code, req))
                {
                    page.Title = "Verified";
                    page.Sections.Add(new(
                        "Verified",
                        [
                            new Subsection(
                                null,
                                [
                                    new Paragraph("You have successfully verified your email address.")
                                ]
                            )
                        ]
                    ));
                }
                else
                {
                    page.Title = "Invalid link";
                    page.Sections.Add(new(
                        "Invalid link",
                        [
                            new Subsection(
                                null,
                                [
                                    new Paragraph("This email verification link is invalid.")
                                ]
                            )
                        ]
                    ));
                }
                
                return page;
            }




            // CHANGE EMAIL DURING VERIFICATION
            case "/verify-change":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        return new RedirectResponse(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        return new RedirectResponse("../2fa" + req.CurrentRedirectQuery);
                    case LoginState.None:
                        return new RedirectResponse("../login" + req.CurrentRedirectQuery);
                    case LoginState.NeedsMailVerification:
                        break;
                    default:
                        return StatusResponse.Forbidden;
                }
                var page = new Page(req, true);
                page.Title = "Change email";
                var emailInput = new TextBox("email", "Enter your email address...", null, TextBoxRole.Email);
                page.Sections.Add(new(
                    "Change email",
                    [
                        new ServerForm(
                            null,
                            async actionReq =>
                            {
                                if (actionReq.HasUser && actionReq.User.MailToken != null)
                                {
                                    if (emailInput.IsEmpty)
                                        return page.DynamicErrorAction("Please enter your email address.");

                                    try
                                    {
                                        await actionReq.UserTable.SetMailAddressAsync(actionReq.User.Id, emailInput.Value);
                                        var user = await actionReq.UserTable.SetNewMailTokenAsync(actionReq.User.Id);
                                        await Presets.WarningMailAsync(req, user, "Welcome", $"Thank you for registering on <a href=\"{req.ProtoHost}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.PluginPathPrefix}/verify-link?user={user.Id}&code={user.MailToken}\">here</a> or enter the following code: {user.MailToken}");
                                    }
                                    catch (Exception ex)
                                    {
                                        return page.DynamicErrorAction(ex.Message);
                                    }
                                }
                                
                                return new Navigate("verify");
                            },
                            [
                                new Heading3("Email"),
                                emailInput,
                                new SubmitButton(new("bi bi-arrow-return-right", "Change"))
                            ]
                        )
                    ]
                ));
                return page;
            }




            // 404
            default:
                return StatusResponse.NotFound;
        }
    }
}