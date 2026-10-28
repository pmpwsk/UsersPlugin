using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/")]
    protected static Page HandleMenu(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true, CommonTexts.Account);
        Section? section = null;
        Subsection? passwordResetSubsection = null;
        passwordResetSubsection = new Subsection(
            CommonTexts.Alert,
            [
                new Paragraph(PluginTexts.PasswortResetAlert),
                new ServerActionButton(CommonTexts.Cancel, async _ =>
                {
                    await req.UserTable.DeleteSettingAsync(req.User.Id, "PasswordReset");
                    if (section != null && passwordResetSubsection != null)
                        section.Subsections.Remove(passwordResetSubsection);
                })
            ]
        );
        var subsection = new Subsection(null);
        section = new Section(CommonTexts.Account, req.User.Settings.ContainsKey("PasswordReset") ? [ passwordResetSubsection, subsection ] : [ subsection ]);
        page.Sections.Add(section);
        if (req.IsAdmin)
            subsection.Content.Add(new BigLinkButton(PluginTexts.ManageUsers, PluginTexts.ManageUsersDescription, "users"));
        subsection.Content.Add(new BigLinkButton(CommonTexts.Logout, PluginTexts.OthersStayLoggedIn, "logout"));
        subsection.Content.Add(new BigLinkButton(PluginTexts.LogoutOthers, PluginTexts.ThisStaysLoggedIn, "logout-others"));
        subsection.Content.Add(new BigLinkButton(CommonTexts.Settings, PluginTexts.SettingsDescription, "settings"));
        return page;
    }
}