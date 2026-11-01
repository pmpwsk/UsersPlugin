using uwap.WebFramework.Accounts;
using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/register")]
    protected static IResponse HandleRegister(Request req)
    {
        req.ForceGET();
        req.ForceNotBanned();
        switch (req.LoginState)
        {
            case LoginState.LoggedIn:
                return new RedirectResponse(req.RedirectUrl);
            case LoginState.Needs2FA:
                return new RedirectResponse("2fa" + req.CurrentRedirectQuery);
            case LoginState.NeedsMailVerification:
                return new RedirectResponse("verify" + req.CurrentRedirectQuery);
        }
        var page = new Page(req, true, CommonTexts.Register);
        page.Sidebar.Items.ReplaceAll(LoginSidebar(req));
        page.Sections.Add(new(
            CommonTexts.Register,
            [
                new ServerForm(
                    null,
                    [
                        new Heading3(CommonTexts.Username),
                        new TextBox("username", PluginTexts.NewUsernamePlaceholder, null, TextBoxRole.Username, true)
                            .Save(out var usernameInput),
                        new Heading3(CommonTexts.Mail),
                        new TextBox("email", CommonTexts.MailPlaceholder, null, TextBoxRole.Email)
                            .Save(out var emailInput),
                        new Heading3(CommonTexts.Password),
                        new TextBox("password1", PluginTexts.NewPasswordPlaceholder, null, TextBoxRole.NewPassword)
                            .Save(out var passwordInput1),
                        new Heading3(PluginTexts.NewPasswordAgain),
                        new TextBox("password2", PluginTexts.NewPasswordAgainPlaceholder, null, TextBoxRole.NewPassword)
                            .Save(out var passwordInput2),
                        new ContinueButton()
                    ],
                    async actionReq =>
                    {
                        if (!actionReq.HasUser)
                        {
                            var username = usernameInput.Value;
                            var email = emailInput.Value;
                            var password1 = passwordInput1.Value;
                            var password2 = passwordInput2.Value;
                            
                            if (password1 != password2)
                                throw new ForcedActionError(PluginTexts.NewPasswordsUnequal);
                            
                            try
                            {
                                User user = await req.UserTable.RegisterAsync(username, email, password1, actionReq);
                                await Presets.WarningMailAsync(req, user, PluginTexts.Welcome, $"{PluginTexts.WelcomeMail1} <a href=\"{req.ProtoHost}\">{req.Domain}</a>.\n{PluginTexts.WelcomeMail2} <a href=\"{req.PluginPathPrefix}/verify-link?user={user.Id}&code={user.MailToken}\">{PluginTexts.Here}</a> {PluginTexts.WelcomeMail3} {user.MailToken}");
                            }
                            catch (Exception ex)
                            {
                                throw new ForcedActionError(ex.Message);
                            }
                        }
                            
                        await page.NavigateAsync("verify" + req.CurrentRedirectQuery);
                    }
                ),
                new Subsection(
                    null,
                    [
                        new BigLinkButton(PluginTexts.LogInInstead, PluginTexts.LogInInsteadDescription, "login" + req.CurrentRedirectQuery)
                    ]
                )
            ]
        ));
        return page;
    }
}