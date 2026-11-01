using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/settings/delete")]
    protected static Page HandleDeleteSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true, PluginTexts.DeleteAccount);
        page.Sidebar.Items.ReplaceAll(SettingsSidebar);
        page.Sections.Add(new(
            PluginTexts.DeleteAccount,
            [
                new ServerForm(
                    null,
                    [
                        new Paragraph(PluginTexts.DeleteAccountIntro1),
                        new Paragraph(req.User.TwoFactor.TOTPEnabled() ? PluginTexts.DeleteAccountIntro2With2FA : PluginTexts.DeleteAccountIntro2Without2FA),
                        ..Presets.CreateAuthElements(req)
                            .Save(out var auth).Elements,
                        new SubmitButton(PluginTexts.DeleteAccountSad)
                    ],
                    async actionReq =>
                    {
                        if (!actionReq.LoggedIn)
                            throw new ForcedActionReload();
                        
                        if (!await Presets.ValidateAuth(actionReq, auth))
                            throw new ForcedActionError(auth.CodeInput != null ? PluginTexts.WrongAuthWith2FA : PluginTexts.WrongAuthWithout2FA);
                        
                        if (actionReq.Cookies.CanWrite)
                            await actionReq.Cookies.DeleteAsync("AuthToken");
                        await actionReq.UserTable.DeleteAllTokensAsync(actionReq.User.Id);
                        await actionReq.UserTable.SetSettingAsync(actionReq.User.Id, "Delete", DateTime.UtcNow.Ticks.ToString());
                        await Presets.WarningMailAsync(actionReq, actionReq.User, PluginTexts.AccountDeleted, PluginTexts.AccountDeletedMail);
                        await page.NavigateAsync("/");
                    }
                )
            ]
        ));
        return page;
    }
}