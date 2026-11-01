using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/settings/username")]
    protected static Page HandleUsernameSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true, CommonTexts.Username);
        page.Sidebar.Items.ReplaceAll(SettingsSidebar);
        page.Sections.Add(new(
            CommonTexts.Username,
            [
                new ServerForm(
                    null,
                    [
                        new Paragraph(PluginTexts.WarningOthersStayLoggedIn),
                        new Paragraph($"{CommonTexts.Current}: {req.User.Username}"),
                        new Heading3(PluginTexts.NewUsername),
                        new TextBox("username", PluginTexts.NewUsernamePlaceholder, null, TextBoxRole.Username) { Autofocus = true }
                            .Save(out var usernameInput),
                        ..Presets.CreateAuthElements(req)
                            .Save(out var auth).Elements,
                        new ContinueButton()
                    ],
                    async actionReq =>
                    {
                        if (!actionReq.LoggedIn)
                            throw new ForcedActionReload();
                        
                        if (usernameInput.IsEmpty(out var username) || auth.AnyEmpty)
                            throw new ForcedActionError(CommonTexts.EmptyFields);
    
                        if (!await Presets.ValidateAuth(actionReq, auth))
                            throw new ForcedActionError(auth.CodeInput != null ? PluginTexts.WrongAuthWith2FA : PluginTexts.WrongAuthWithout2FA);

                        try
                        {
                            await actionReq.UserTable.SetUsernameAsync(actionReq.User.Id, username);
                            await Presets.WarningMailAsync(actionReq, actionReq.User, PluginTexts.UsernameChanged, $"{PluginTexts.UsernameChangedMail1}{username}{PluginTexts.UsernameChangedMail2}");
                            await page.NavigateAsync("../settings");
                        }
                        catch (Exception ex)
                        {
                            throw new ForcedActionError(ex.Message);
                        }
                    }
                )
            ]
        ));
        return page;
    }
}