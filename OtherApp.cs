using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public static void Other(AppRequest request, string path, string pathPrefix)
    {
        request.Init(out Page page, out List<IPageElement> e);
        switch (path)
        {
            case "":
                page.Title = "Account";
                if (NotLoggedIn(request)) break;
                e.Add(new HeadingElement("Account", ""));
                if (request.IsAdmin())
                    e.Add(new ButtonElement("Manage users", null, $"{pathPrefix}/users"));
                e.Add(new ButtonElement("Log out", null, $"{pathPrefix}/logout"));
                e.Add(new ButtonElement("Log out all other devices", null, $"{pathPrefix}/logout-others"));
                e.Add(new ButtonElement("Settings", null, $"{pathPrefix}/settings"));
                break;
            case "/login":
                page.Title = "Login";
                if (AlreadyLoggedIn(request)) break;
                page.Scripts.Add(Presets.RedirectScript);
                page.Scripts.Add(new Script($"{pathPrefix}/login.js"));
                e.Add(new HeadingElement("Login", ""));
                e.Add(new ContainerElement(null, new List<IContent>
                {
                    new Heading("Username:"),
                    new TextBox("Enter your username...", null, "username", TextBoxRole.Username, "Continue()", autofocus: true),
                    new Heading("Password:"),
                    new TextBox("Enter your password...", null, "password", TextBoxRole.Password, "Continue()"),
                }));
                e.Add(new ButtonElementJS("Continue", null, "Continue()", id: "continueButton"));
                Presets.AddError(page);
                e.Add(new ButtonElement(null, "Account recovery", $"{pathPrefix}/recovery" + request.CurrentRedirectQuery()));
                e.Add(new ButtonElement(null, "Register instead", $"{pathPrefix}/register" + request.CurrentRedirectQuery()));
                break;
            case "/register":
                page.Title = "Register";
                if (AlreadyLoggedIn(request)) break;
                page.Scripts.Add(Presets.RedirectScript);
                page.Scripts.Add(new Script($"{pathPrefix}/register.js"));
                e.Add(new HeadingElement("Register", ""));
                e.Add(new ContainerElement(null, new List<IContent>
                {
                    new Heading("Username:"),
                    new TextBox("Enter a username...", null, "username", TextBoxRole.Username, "Continue()", autofocus: true),
                    new Heading("Email:"),
                    new TextBox("Enter your email address...", null, "email", TextBoxRole.Email, "Continue()"),
                    new Heading("Password:"),
                    new TextBox("Enter a password...", null, "password1", TextBoxRole.NewPassword, "Continue()"),
                    new Heading("Confirm password:"),
                    new TextBox("Enter the password again...", null, "password2", TextBoxRole.NewPassword, "Continue()")
                }));
                e.Add(new ButtonElementJS("Continue", null, "Continue()", id: "continueButton"));
                Presets.AddError(page);
                e.Add(new ButtonElement(null, "Log in instead", $"{pathPrefix}/login" + request.CurrentRedirectQuery()));
                break;
            case "/verify":
                {
                    page.Title = "Verify";
                    if (request.LoginState != LoginState.NeedsMailVerification || request.User.MailToken == null
                        || (request.Query.ContainsKey("code") && request.User.VerifyMail(request.Query["code"], request)))
                    {
                        request.Redirect(request.RedirectUrl);
                        break;
                    }
                    page.Scripts.Add(Presets.RedirectScript);
                    if (request.Query.ContainsKey("change"))
                    {
                        page.Scripts.Add(new Script($"{pathPrefix}/verify-change.js"));
                        e.Add(new HeadingElement("Change email address", ""));
                        e.Add(new ContainerElement(null,
                        [
                            new Heading("Email:"),
                            new TextBox("Enter your email address...", request.User.MailAddress, "email", onEnter: "Continue()", autofocus: true)
                        ]));
                        e.Add(new ButtonElementJS("Change", null, "Continue()"));
                        Presets.AddError(page);
                        e.Add(new ButtonElement(null, "Back", $"{pathPrefix}/verify" + request.CurrentRedirectQuery()));
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
                        string query = request.CurrentRedirectQuery();
                        if (query == "") query = "?";
                        else query += "&";
                        query += "change=true";
                        e.Add(new ButtonElement(null, "Change email address", $"{pathPrefix}/verify" + query));
                        e.Add(new ButtonElement(null, "Log out instead", $"{pathPrefix}/logout" + request.CurrentRedirectQuery()));
                    }
                }
                break;
            case "/2fa":
                page.Title = "2FA";
                if (request.LoginState != LoginState.Needs2FA)
                {
                    request.Redirect(request.RedirectUrl);
                    break;
                }
                page.Scripts.Add(Presets.RedirectScript);
                page.Scripts.Add(new Script($"{pathPrefix}/2fa.js"));
                e.Add(new HeadingElement("Two-factor authentication", ""));
                e.Add(new ContainerElement(null, new List<IContent>
                {
                    new Heading("2FA code / recovery:"),
                    new TextBox("Enter the current code...", null, "code", TextBoxRole.Username, "Continue()", autofocus: true)
                }));
                e.Add(new ButtonElementJS("Continue", null, "Continue()"));
                Presets.AddError(page);
                e.Add(new ButtonElement(null, "Log out instead", $"{pathPrefix}/logout" + request.CurrentRedirectQuery()));
                break;
            case "/logout":
                request.UserTable.Logout(request);
                request.Redirect(request.RedirectUrl);
                break;
            case "/logout-others":
                if (NotLoggedIn(request)) break;
                request.UserTable.LogoutOthers(request);
                e.Add(new HeadingElement("Success", "Successfully logged out all other devices and browsers."));
                e.Add(new ButtonElement("Back to account", null, pathPrefix == "" ? "/" : pathPrefix));
                break;
            default:
                request.Status = 404;
                break;
        }
    }
}