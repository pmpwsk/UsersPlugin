using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/settings/password")]
    protected static Page HandlePasswordSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true, CommonTexts.Password);
        page.Sidebar.Items.ReplaceAll(SettingsSidebar);
        page.Sections.Add(new(
            CommonTexts.Password,
            [
                new ServerForm(
                    null,
                    [
                        new Paragraph(PluginTexts.WarningOthersStayLoggedIn),
                        new Heading3(PluginTexts.NewPassword),
                        new TextBox("password1", PluginTexts.NewPasswordPlaceholder, null, TextBoxRole.NewPassword) { Autofocus = true }
                            .Save(out var passwordInput1),
                        new Heading3(PluginTexts.NewPasswordAgain),
                        new TextBox("password2", PluginTexts.NewPasswordAgainPlaceholder, null, TextBoxRole.NewPassword)
                            .Save(out var passwordInput2),
                        ..Presets.CreateAuthElements(req, true)
                            .Save(out var auth).Elements,
                        new ContinueButton()
                    ],
                    async actionReq =>
                    {
                        if (!actionReq.LoggedIn)
                            throw new ForcedActionReload();
                        
                        if (passwordInput1.IsEmpty(out var password1) || passwordInput2.IsEmpty(out var password2) || auth.AnyEmpty)
                            throw new ForcedActionError(CommonTexts.EmptyFields);
                        
                        if (password1 != password2)
                            throw new ForcedActionError(PluginTexts.NewPasswordsUnequal);
                        
                        if (!await Presets.ValidateAuth(actionReq, auth))
                            throw new ForcedActionError(auth.CodeInput != null ? PluginTexts.WrongAuthWith2FA : PluginTexts.WrongAuthWithout2FA);
                        
                        try
                        {
                            await req.UserTable.SetPasswordAsync(req.User.Id, password1);
                            await Presets.WarningMailAsync(req, req.User, PluginTexts.PasswordChanged, "Your password was just changed.");
                            page.Navigate("../settings");
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