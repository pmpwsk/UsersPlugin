using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public static void Recovery(AppRequest request, string path, string pathPrefix)
    {
        if (AlreadyLoggedIn(request)) return;
        request.Init(out Page page, out List<IPageElement> e);
        switch (path)
        {
            case "":
                page.AddTitle("Recovery", "Here are some options to recover your account. If you still can't log in, please contact our support.");
                e.Add(new ButtonElement("Username", null, $"{pathPrefix}/recovery/username" + request.CurrentRedirectQuery()));
                e.Add(new ButtonElement("Password", null, $"{pathPrefix}/recovery/password" + request.CurrentRedirectQuery()));
                e.Add(new ButtonElement("Two-factor authentication", null, $"{pathPrefix}/recovery/2fa" + request.CurrentRedirectQuery()));
                page.AddSupportButton();
                break;
            case "/username":
                {
                    page.AddTitle("Username recovery", "Enter your email address below and you'll receive an email telling you your username.");
                    request.AddScript();
                    string command = $"Continue('{pathPrefix}/login{request.CurrentRedirectQuery()}')";
                    e.Add(new ContainerElement("Email:", new TextBox("Enter your email address...", null, "email", TextBoxRole.Email, command)));
                    e.Add(new ButtonElementJS("Continue", null, command));
                    page.AddError();
                }
                break;
            case "/password":
                {
                    if (request.Query.ContainsKey("token"))
                    {
                        string token = request.Query["token"];
                        if (token.Length == 30)
                        {
                            string id = token.Remove(12);
                            string code = token.Remove(0, 12);
                            var users = request.UserTable;
                            if (users.ContainsKey(id))
                            {
                                User user = users[id];
                                if (user.Settings.ContainsKey("PasswordReset") && user.Settings["PasswordReset"] == code)
                                {
                                    //setting a new password
                                    page.AddTitle("Password recovery", "Enter a new password and confirm it below.");
                                    request.AddScript("set");
                                    string cmd = $"Continue('{pathPrefix}/login{request.CurrentRedirectQuery()}', '{token}')";
                                    e.Add(new ContainerElement(null, new List<IContent>
                                {
                                    new Heading("New password:"),
                                    new TextBox("Enter a password...", null, "password1", TextBoxRole.NewPassword, cmd),
                                    new Heading("Confirm password:"),
                                    new TextBox("Enter the password again...", null, "password2", TextBoxRole.NewPassword, cmd)
                                }));
                                    e.Add(new ButtonElementJS("Continue", null, cmd));
                                    page.AddError();
                                    break;
                                }
                            }
                        }
                    }
                    //requesting a password link
                    page.AddTitle("Password recovery", "Enter your email address below and you'll receive an email with a link to reset your password.");
                    request.AddScript("request");
                    string command = $"Continue('{pathPrefix}/login{request.CurrentRedirectQuery()}')";
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
                request.Status = 404;
                break;
        }
    }
}