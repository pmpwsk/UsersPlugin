using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public static void Recovery(AppRequest req, string path, string pathPrefix)
    {
        if (AlreadyLoggedIn(req))
            return;
        req.Init(out Page page, out List<IPageElement> e);
        switch (path)
        {
            case "":
                page.AddTitle("Recovery", "Here are some options to recover your account. If you still can't log in, please contact our support.");
                e.Add(new ButtonElement("Username", null, $"{pathPrefix}/recovery/username" + req.CurrentRedirectQuery()));
                e.Add(new ButtonElement("Password", null, $"{pathPrefix}/recovery/password" + req.CurrentRedirectQuery()));
                e.Add(new ButtonElement("Two-factor authentication", null, $"{pathPrefix}/recovery/2fa" + req.CurrentRedirectQuery()));
                page.AddSupportButton();
                break;
            case "/username":
                {
                    page.AddTitle("Username recovery", "Enter your email address below and you'll receive an email telling you your username.");
                    page.Scripts.Add(new Script($"{pathPrefix}/recovery/username.js"));
                    string command = $"Continue('{pathPrefix}/login{req.CurrentRedirectQuery()}')";
                    e.Add(new ContainerElement("Email:", new TextBox("Enter your email address...", null, "email", TextBoxRole.Email, command)));
                    e.Add(new ButtonElementJS("Continue", null, command));
                    page.AddError();
                }
                break;
            case "/password":
                {
                    if (req.Query.TryGetValue("token", out var token) && token.Length == 30)
                    {
                        string id = token.Remove(12);
                        string code = token.Remove(0, 12);
                        if (req.UserTable.TryGetValue(id, out var user) && user.Settings.TryGet("PasswordReset") == code)
                        {
                            //setting a new password
                            page.AddTitle("Password recovery", "Enter a new password and confirm it below.");
                            page.Scripts.Add(new Script($"{pathPrefix}/recovery/password-set.js"));
                            string cmd = $"Continue('{pathPrefix}/login{req.CurrentRedirectQuery()}', '{token}')";
                            e.Add(new ContainerElement(null,
                            [
                                new Heading("New password:"),
                                new TextBox("Enter a password...", null, "password1", TextBoxRole.NewPassword, cmd),
                                new Heading("Confirm password:"),
                                new TextBox("Enter the password again...", null, "password2", TextBoxRole.NewPassword, cmd)
                            ]));
                            e.Add(new ButtonElementJS("Continue", null, cmd));
                            page.AddError();
                            break;
                        }
                    }
                    //requesting a password link
                    page.AddTitle("Password recovery", "Enter your email address below and you'll receive an email with a link to reset your password.");
                    page.Scripts.Add(new Script($"{pathPrefix}/recovery/password-request.js"));
                    string command = $"Continue('{pathPrefix}/login{req.CurrentRedirectQuery()}')";
                    e.Add(new ContainerElement("Email:", new TextBox("Enter your email address...", null, "email", TextBoxRole.Email, command)));
                    e.Add(new ButtonElementJS("Continue", null, command));
                    page.AddError();
                }
                break;
            case "/2fa":
                page.AddTitle("2FA recovery", "If you lost access to your 2FA app, you can use one of the recovery codes you were given when you enabled 2FA. If you've lost those as well, please contact our support.");
                page.AddSupportButton();
                break;
            default:
                req.Status = 404;
                break;
        }
    }
}