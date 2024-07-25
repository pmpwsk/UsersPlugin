using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    private static async Task HandleRecovery(Request req)
    {
        switch (req.Path)
        {
            // MAIN USER MANAGEMENT PAGE
            case "/recovery":
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
                req.CreatePage("Recovery", out var page, out var e);
                page.Navigation.Add(new Button("Back", $"login{req.CurrentRedirectQuery}", "right"));
                e.Add(new HeadingElement("Recovery", "Here are some options to recover your account. If you still can't log in, please contact our support."));
                e.Add(new ButtonElement("Username", null, $"recovery/username{req.CurrentRedirectQuery}"));
                e.Add(new ButtonElement("Password", null, $"recovery/password{req.CurrentRedirectQuery}"));
                e.Add(new ButtonElement("Two-factor authentication", null, $"recovery/2fa{req.CurrentRedirectQuery}"));
                page.AddSupportButton(req);
            } break;




            // REQUEST USERNAME
            case "/recovery/username":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        throw new RedirectSignal(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        throw new RedirectSignal("../2fa" + req.CurrentRedirectQuery);
                    case LoginState.NeedsMailVerification:
                        throw new RedirectSignal("../verify" + req.CurrentRedirectQuery);
                }
                req.CreatePage("Username recovery", out var page, out var e);
                page.Navigation.Add(new Button("Back", $"../recovery{req.CurrentRedirectQuery}", "right"));
                page.Scripts.Add(Presets.RedirectQueryScript);
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("username.js"));
                e.Add(new HeadingElement("Username recovery", "Enter your email address below and you'll receive an email telling you your username."));
                string command = "Continue()";
                e.Add(new ContainerElement("Email:", new TextBox("Enter your email address...", null, "email", TextBoxRole.Email, command)));
                e.Add(new ButtonElementJS("Continue", null, command));
                page.AddError();
            } break;
            
            case "/recovery/username/request":
            { req.ForcePOST();
                if (req.HasUser)
                    await req.Write("ok");
                if (!req.Query.TryGetValue("email", out var email))
                    throw new BadRequestSignal();
                User? user = req.UserTable.FindByMailAddress(email);
                if (user == null)
                {
                    AccountManager.ReportFailedAuth(req.Context);
                    await req.Write("no");
                    break;
                }
                Presets.WarningMail(req, user, "Username recovery", $"You requested your username, it is: {user.Username}");
                await req.Write("ok");
            }
            break;




            // RESET PASSWORD
            case "/recovery/password":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        throw new RedirectSignal(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        throw new RedirectSignal("../2fa" + req.CurrentRedirectQuery);
                    case LoginState.NeedsMailVerification:
                        throw new RedirectSignal("../verify" + req.CurrentRedirectQuery);
                }
                req.CreatePage("Password recovery", out var page, out var e);
                page.Navigation.Add(new Button("Back", $"../recovery{req.CurrentRedirectQuery}", "right"));
                page.Scripts.Add(Presets.RedirectQueryScript);
                page.Scripts.Add(Presets.SendRequestScript);
                if (req.Query.TryGetValue("token", out var token) && token.Length == 76)
                {
                    string id = token[..12];
                    string code = token[12..];
                    if (req.UserTable.TryGetValue(id, out var user) && user.Settings.TryGet("PasswordReset") == code)
                    {
                        //setting a new password
                        e.Add(new HeadingElement("Password recovery", "Enter a new password and confirm it below."));
                        page.Scripts.Add(new Script("password/set.js"));
                        e.Add(new ContainerElement(null,
                        [
                            new Heading("New password:"),
                            new TextBox("Enter a password...", null, "password1", TextBoxRole.NewPassword, "Continue()"),
                            new Heading("Confirm password:"),
                            new TextBox("Enter the password again...", null, "password2", TextBoxRole.NewPassword, "Continue()")
                        ]));
                        e.Add(new ButtonElementJS("Continue", null, "Continue()"));
                        page.AddError();
                        break;
                    }
                }
                //requesting a password link
                e.Add(new HeadingElement("Password recovery", "Enter your email address below and you'll receive an email with a link to reset your password."));
                page.Scripts.Add(new Script("password/request.js"));
                e.Add(new ContainerElement("Email:", new TextBox("Enter your email address...", null, "email", TextBoxRole.Email, "Continue()")));
                e.Add(new ButtonElementJS("Continue", null, "Continue()"));
                page.AddError();
            } break;

            case "/recovery/password/request":
            { req.ForcePOST();
                if (req.HasUser)
                    await req.Write("ok");
                if (!req.Query.TryGetValue("email", out var email))
                    throw new BadRequestSignal();
                User? user = req.UserTable.FindByMailAddress(email);
                if (user == null)
                {
                    AccountManager.ReportFailedAuth(req.Context);
                    await req.Write("no");
                    break;
                }
                string code = Parsers.RandomString(64);
                user.Settings["PasswordReset"] = code;
                string url = $"{req.PluginPathPrefix}/recovery/password?token={user.Id}{code}";
                Presets.WarningMail(req, user, "Password recovery", $"You requested password recovery. Open the following link to reset your password:\n<a href=\"{url}\">{url}</a>\nYou can cancel the password reset from Account > Settings > Password.");
                await req.Write("ok");
            } break;

            case "/recovery/password/set":
            { req.ForcePOST();
                if (req.HasUser)
                    await req.Write("ok");
                if (!(req.Query.TryGetValue("password", out var password) && req.Query.TryGetValue("token", out var token) && token.Length == 76))
                    throw new BadRequestSignal();
                string id = token.Remove(12);
                string code = token.Remove(0, 12);
                var users = req.UserTable;
                if (!(users.TryGetValue(id, out var user) && user.Settings.TryGetValue("PasswordReset", out var existingCode)))
                    throw new NotFoundSignal();
                if (existingCode != code)
                {
                    AccountManager.ReportFailedAuth(req.Context);
                    req.Status = 400;
                    break;
                }
                try
                {
                    user.SetPassword(password);
                    Presets.WarningMail(req, user, "Password recovery", "Your password was just changed by recovery.");
                    user.Settings.Delete("PasswordReset");
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
            } break;




            // 2FA RECOVERY INFORMATION
            case "/recovery/2fa":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        throw new RedirectSignal(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        throw new RedirectSignal("../2fa" + req.CurrentRedirectQuery);
                    case LoginState.NeedsMailVerification:
                        throw new RedirectSignal("../verify" + req.CurrentRedirectQuery);
                }
                req.CreatePage("2FA recovery", out var page, out var e);
                page.Navigation.Add(new Button("Back", $"../recovery{req.CurrentRedirectQuery}", "right"));
                e.Add(new HeadingElement("2FA recovery", "If you lost access to your 2FA app, you can use one of the recovery codes you were given when you enabled 2FA. If you've lost those as well, please contact our support."));
                page.AddSupportButton(req);
            } break;
            



            // 404
            default:
                req.CreatePage("Error");
                req.Status = 404;
                break;
        }
    }
}