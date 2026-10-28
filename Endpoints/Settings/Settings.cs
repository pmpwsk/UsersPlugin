using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/settings")]
    protected static Page HandleSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        return new Page(
            req, false,
            CommonTexts.Settings,
            MainSidebar(req),
            [
                new Section(
                    CommonTexts.Settings,
                    [
                        new Subsection(
                            null,
                            [
                                new BigLinkButton(
                                    CommonTexts.Username,
                                    PluginTexts.UsernameSettingsDescription,
                                    "settings/username"
                                ),
                                new BigLinkButton(
                                    CommonTexts.Mail,
                                    PluginTexts.MailSettingsDescription,
                                    "settings/email"
                                ),
                                new BigLinkButton(
                                    CommonTexts.Password,
                                    PluginTexts.PasswordSettingsDescription,
                                    "settings/password"
                                ),
                                new BigLinkButton(
                                    PluginTexts.TwoFactorLong,
                                    req.User.TwoFactor.TOTPEnabled()
                                        ? PluginTexts.DisableTwoFactorDescription
                                        : PluginTexts.EnableTwoFactorDescription,
                                    "settings/2fa"
                                ),
                                new BigLinkButton(
                                    PluginTexts.Applications,
                                    PluginTexts.ManageApplicationsDescription,
                                    "settings/apps"
                                ),
                                new BigLinkButton(
                                    PluginTexts.DeleteAccount,
                                    PluginTexts.DeleteAccountDescription,
                                    "settings/delete"
                                )
                            ]
                        )
                    ]
                )
            ]
        );
    }
}