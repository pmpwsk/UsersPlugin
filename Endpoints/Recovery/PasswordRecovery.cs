using uwap.WebFramework.Accounts;
using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/recovery/password")]
    protected static IResponse HandlePasswordRecovery(Request req)
    {
        req.ForceGET();
        req.ForceNotBanned();
        switch (req.LoginState)
        {
            case LoginState.LoggedIn:
                return new RedirectResponse(req.RedirectUrl);
            case LoginState.Needs2FA:
                return new RedirectResponse("../2fa" + req.CurrentRedirectQuery);
            case LoginState.NeedsMailVerification:
                return new RedirectResponse("../verify" + req.CurrentRedirectQuery);
        }
        var page = new Page(req, true, PluginTexts.PasswordRecovery);
        page.Sidebar.Items.ReplaceAll(RecoverySidebar(req));
        page.Sections.Add(new(
            PluginTexts.PasswordRecovery,
            [
                new ServerForm(
                    null,
                    [
                        new Paragraph(PluginTexts.PasswordRecoveryIntro),
                        new Heading3(CommonTexts.Mail),
                        new TextBox("email", CommonTexts.MailPlaceholder, null, TextBoxRole.Email) { Autofocus = true }
                            .Save(out var emailInput),
                        new ContinueButton()
                    ],
                    async actionReq =>
                    {
                        if (!actionReq.HasUser)
                        {
                            if (emailInput.IsEmpty(out var email))
                                throw new ForcedActionError(CommonTexts.MailEmpty);

                            User? user = await actionReq.UserTable.FindByMailAddressAsync(email);
                            if (user == null)
                            {
                                AccountManager.ReportFailedAuth(actionReq);
                                throw new ForcedActionError(CommonTexts.MailNotFound);
                            }
                            
                            string code = Parsers.RandomString(64);
                            await actionReq.UserTable.SetSettingAsync(user.Id, "PasswordReset", code);
                            string url = $"{req.PluginPathPrefix}/recovery/password-set?token={user.Id}{code}";
                            await Presets.WarningMailAsync(actionReq, user, PluginTexts.PasswordRecovery, $"{PluginTexts.PasswordRecoveryMail1}\n<a href=\"{url}\">{url}</a>\n{PluginTexts.PasswordRecoveryMail2}");
                        }

                        await page.NavigateAsync("../login" + req.CurrentRedirectQuery);
                    }
                )
            ]
        ));
        return page;
    }
}