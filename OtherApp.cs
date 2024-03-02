using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public static void Other(AppRequest req, string path, string pathPrefix)
    {
        req.Init(out Page page, out List<IPageElement> e);
        switch (path)
        {
            case "":
                page.Title = "Account";
                if (NotLoggedIn(req))
                    break;
                e.Add(new HeadingElement("Account", ""));
                if (req.IsAdmin())
                    e.Add(new ButtonElement("Manage users", null, $"{pathPrefix}/users"));
                e.Add(new ButtonElement("Log out", null, $"{pathPrefix}/logout"));
                e.Add(new ButtonElement("Log out all other devices", null, $"{pathPrefix}/logout-others"));
                e.Add(new ButtonElement("Settings", null, $"{pathPrefix}/settings"));
                break;
            case "/login":
                page.Title = "Login";
                if (AlreadyLoggedIn(req))
                    break;
                page.Scripts.Add(Presets.RedirectScript);
                page.Scripts.Add(new Script($"{pathPrefix}/login.js"));
                e.Add(new HeadingElement("Login", ""));
                e.Add(new ContainerElement(null,
                [
                    new Heading("Username:"),
                    new TextBox("Enter your username...", null, "username", TextBoxRole.Username, "Continue()", autofocus: true),
                    new Heading("Password:"),
                    new TextBox("Enter your password...", null, "password", TextBoxRole.Password, "Continue()"),
                ]));
                e.Add(new ButtonElementJS("Continue", null, "Continue()", id: "continueButton"));
                Presets.AddError(page);
                e.Add(new ButtonElement(null, "Account recovery", $"{pathPrefix}/recovery" + req.CurrentRedirectQuery()));
                e.Add(new ButtonElement(null, "Register instead", $"{pathPrefix}/register" + req.CurrentRedirectQuery()));
                break;
            case "/register":
                page.Title = "Register";
                if (AlreadyLoggedIn(req)) break;
                page.Scripts.Add(Presets.RedirectScript);
                page.Scripts.Add(new Script($"{pathPrefix}/register.js"));
                e.Add(new HeadingElement("Register", ""));
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
                e.Add(new ButtonElement(null, "Log in instead", $"{pathPrefix}/login" + req.CurrentRedirectQuery()));
                break;
            case "/verify":
                {
                    page.Title = "Verify";
                    if (req.Query.TryGetValue("user", out var uid) && req.Query.TryGetValue("code", out var code))
                    {
                        User user;
                        if (req.HasUser && req.User.Id == uid)
                            user = req.User;
                        else if (req.UserTable.TryGetValue(uid, out var u))
                            user = u;
                        else goto Skip;
                        
                        if (user.MailToken == null)
                            req.Redirect(req.RedirectUrl);
                        else if (user.VerifyMail(code, req))
                            e.Add(new HeadingElement("Verified!", "You have successfully verified your email address.", "green"));
                        else goto Skip;
                        
                        break;
                    }
                Skip:
                    if (req.LoginState != LoginState.NeedsMailVerification || req.User.MailToken == null)
                    {
                        req.Redirect(req.RedirectUrl);
                        break;
                    }
                    page.Scripts.Add(Presets.RedirectScript);
                    if (req.Query.ContainsKey("change"))
                    {
                        page.Scripts.Add(new Script($"{pathPrefix}/verify-change.js"));
                        e.Add(new HeadingElement("Change email address", ""));
                        e.Add(new ContainerElement(null,
                        [
                            new Heading("Email:"),
                            new TextBox("Enter your email address...", req.User.MailAddress, "email", onEnter: "Continue()", autofocus: true)
                        ]));
                        e.Add(new ButtonElementJS("Change", null, "Continue()"));
                        Presets.AddError(page);
                        e.Add(new ButtonElement(null, "Back", $"{pathPrefix}/verify" + req.CurrentRedirectQuery()));
                    }
                    else
                    {
                        page.Scripts.Add(new Script($"{pathPrefix}/verify.js"));
                        e.Add(new HeadingElement("Mail verification", ""));
                        e.Add(new ContainerElement(null,
                        [
                            new Heading("Verification code:"),
                            new TextBox("Enter the code...", null, "code", onEnter: "Continue()", autofocus: true)
                        ]) { Buttons = [ new ButtonJS("Send again", "Resend()") ] });
                        e.Add(new ButtonElementJS("Continue", null, "Continue()"));
                        Presets.AddError(page);
                        string query = req.CurrentRedirectQuery();
                        if (query == "") query = "?";
                        else query += "&";
                        query += "change=true";
                        e.Add(new ButtonElement(null, "Change email address", $"{pathPrefix}/verify" + query));
                        e.Add(new ButtonElement(null, "Log out instead", $"{pathPrefix}/logout" + req.CurrentRedirectQuery()));
                    }
                }
                break;
            case "/2fa":
                page.Title = "2FA";
                if (req.LoginState != LoginState.Needs2FA)
                {
                    req.Redirect(req.RedirectUrl);
                    break;
                }
                page.Scripts.Add(Presets.RedirectScript);
                page.Scripts.Add(new Script($"{pathPrefix}/2fa.js"));
                e.Add(new HeadingElement("Two-factor authentication", ""));
                e.Add(new ContainerElement(null,
                [
                    new Heading("2FA code / recovery:"),
                    new TextBox("Enter the current code...", null, "code", TextBoxRole.Username, "Continue()", autofocus: true)
                ]));
                e.Add(new ButtonElementJS("Continue", null, "Continue()"));
                Presets.AddError(page);
                e.Add(new ButtonElement(null, "Log out instead", $"{pathPrefix}/logout" + req.CurrentRedirectQuery()));
                break;
            case "/logout":
                req.UserTable.Logout(req);
                req.Redirect(req.RedirectUrl);
                break;
            case "/logout-others":
                if (NotLoggedIn(req)) break;
                req.UserTable.LogoutOthers(req);
                e.Add(new HeadingElement("Success", "Successfully logged out all other devices and browsers."));
                e.Add(new ButtonElement("Back to account", null, pathPrefix == "" ? "/" : pathPrefix));
                break;
            case "/auth-request":
                {
                    if (req.Query.TryGetValue("name", out var name) && name != "" && name == name.HtmlSafe() && req.Query.TryGetValue("yes", out var yes) && req.Query.TryGetValue("no", out var no) && req.Query.TryGetValue("allowed", out var limitedToPathsEncoded))
                    {
                        var limitedToPaths = limitedToPathsEncoded.Split(',').Select(x => HttpUtility.UrlDecode(x).HtmlSafe()).ToList();
                        if (limitedToPaths.Contains(""))
                        {
                            req.Status = 400;
                            break;
                        }

                        page.Scripts.Add(new Script($"{pathPrefix}/auth-request.js"));

                        string? backgroundDomain = req.Query.TryGetValue("background", out var b) && b.SplitAtFirst("://", out var bProto, out b) && (bProto == "http" || bProto == "https") && b.SplitAtFirst('/', out b, out _) ? b : null;

                        page.Title = "Authentication request";
                        if (NotLoggedIn(req)) break;
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
                            $"In order to be able to log themselves out, applications automatically get access to \"{req.Domain}{pathPrefix}/logout\" as well."
                        ]));
                    }
                    else req.Status = 400;
                } break;
            default:
                req.Status = 404;
                break;
        }
    }
}