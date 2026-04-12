using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static IResponse HandleLogin(Request req)
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
        var page = new Page(req, true, "Login");
        page.Sidebar.Items.ReplaceAll(LoginSidebar(req));
        page.Sections.Add(new(
            "Login",
            [
                new ServerForm(
                    null,
                    [
                        new Heading3("Username"),
                        new TextBox("username", "Enter your username...", null, TextBoxRole.Username) { Autofocus = true }
                            .Save(out var usernameInput),
                        new Heading3("Password"),
                        new TextBox("password", "Enter your password...", null, TextBoxRole.CurrentPassword)
                            .Save(out var passwordInput),
                        new ContinueButton()
                    ],
                    async actionReq =>
                    {
                        if (actionReq.HasUser)
                            return new Navigate(req.RedirectUrl);
                        if (usernameInput.IsEmpty(out var username) || passwordInput.IsEmpty(out var password))
                            return DialogBuilder.DynamicErrorAction(page, "Please enter your username and password.");

                        User? user = await actionReq.UserTable.LoginAsync(username, password, actionReq);
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
                            return DialogBuilder.DynamicErrorAction(page, "The combination of username and password you have entered isn't correct.");
                    }
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
}