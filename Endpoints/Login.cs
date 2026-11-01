using uwap.WebFramework.Accounts;
using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/login")]
    protected static IResponse HandleLogin(Request req)
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
        var page = new Page(req, true, CommonTexts.Login);
        page.Sidebar.Items.ReplaceAll(LoginSidebar(req));
        page.Sections.Add(new(
            CommonTexts.Login,
            [
                new ServerForm(
                    null,
                    [
                        new Heading3(CommonTexts.Username),
                        new TextBox("username", CommonTexts.UsernamePlaceholder, null, TextBoxRole.Username, true)
                            .Save(out var usernameInput),
                        new Heading3(CommonTexts.Password),
                        new TextBox("password", CommonTexts.PasswordPlaceholder, null, TextBoxRole.CurrentPassword)
                            .Save(out var passwordInput),
                        new ContinueButton()
                    ],
                    async actionReq =>
                    {
                        if (actionReq.HasUser)
                            await page.NavigateAsync(req.RedirectUrl);
                        
                        if (usernameInput.IsEmpty(out var username) || passwordInput.IsEmpty(out var password))
                            throw new ForcedActionError(CommonTexts.EmptyFields);
                        
                        User? user = await actionReq.UserTable.LoginAsync(username, password, actionReq);
                        if (user == null)
                            throw new ForcedActionError(PluginTexts.WrongLogin);
                        
                        if (user.Settings.ContainsKey("Delete"))
                            await actionReq.UserTable.DeleteSettingAsync(user.Id, "Delete");
                        if (user.TwoFactor.TOTPEnabled())
                            await page.NavigateAsync("2fa" + req.CurrentRedirectQuery);
                        else
                        {
                            await Presets.WarningMailAsync(actionReq, user, PluginTexts.NewLogin, PluginTexts.NewLoginMail);
                            if (user.MailToken == null)
                                await page.NavigateAsync(req.RedirectUrl);
                            else
                                await page.NavigateAsync("verify" + req.CurrentRedirectQuery);
                        }
                    }
                ),
                new Subsection(
                    null,
                    [
                        new BigLinkButton(
                            PluginTexts.AccountRecovery,
                            PluginTexts.AccountRecoveryDescription,
                            $"recovery{req.CurrentRedirectQuery}"
                        ),
                        new BigLinkButton(
                            PluginTexts.RegisterInstead,
                            PluginTexts.RegisterInsteadDescription,
                            $"register{req.CurrentRedirectQuery}"
                        )
                    ]
                )
            ]
        ));
        return page;
    }
}