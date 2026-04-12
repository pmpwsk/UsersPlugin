using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static IResponse HandleRegister(Request req)
    {
        req.ForceGET();
        switch (req.LoginState)
        {
            case LoginState.LoggedIn:
                return new RedirectResponse(req.RedirectUrl);
            case LoginState.Needs2FA:
                return new RedirectResponse("2fa" + req.CurrentRedirectQuery);
            case LoginState.NeedsMailVerification:
                return new RedirectResponse("verify" + req.CurrentRedirectQuery);
        }
        var page = new Page(req, true, "Register");
        page.Sidebar.Items.ReplaceAll(LoginSidebar(req));
        page.Sections.Add(new(
            "Register",
            [
                new ServerForm(
                    null,
                    [
                        new Heading3("Username"),
                        new TextBox("username", "Enter a username...", null, TextBoxRole.Username) { Autofocus = true }
                            .Save(out var usernameInput),
                        new Heading3("Email"),
                        new TextBox("email", "Enter your email address...", null, TextBoxRole.Email)
                            .Save(out var emailInput),
                        new Heading3("Password"),
                        new TextBox("password1", "Enter a password...", null, TextBoxRole.NewPassword)
                            .Save(out var passwordInput1),
                        new Heading3("Confirm password"),
                        new TextBox("password2", "Enter the password again...", null, TextBoxRole.NewPassword)
                            .Save(out var passwordInput2),
                        new ContinueButton()
                    ],
                    async actionReq =>
                    {
                        if (!actionReq.HasUser)
                        {
                            if (usernameInput.IsEmpty(out var username) || emailInput.IsEmpty(out var email) || passwordInput1.IsEmpty(out var password1) || passwordInput2.IsEmpty(out var password2))
                                return page.DynamicErrorAction("Please fill out all fields.");
                            if (password1 != password2)
                                return page.DynamicErrorAction("The passwords do not match.");
                            
                            try
                            {
                                User user = await req.UserTable.RegisterAsync(username, email, password1, actionReq);
                                await Presets.WarningMailAsync(req, user, "Welcome", $"Thank you for registering on <a href=\"{req.ProtoHost}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.PluginPathPrefix}/verify-link?user={user.Id}&code={user.MailToken}\">here</a> or enter the following code: {user.MailToken}");
                            }
                            catch (Exception ex)
                            {
                                return page.DynamicErrorAction(ex.Message);
                            }
                        }
                            
                        return new Navigate("verify" + req.CurrentRedirectQuery);
                    }
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
}