using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses.Base;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    public static List<AbstractElement> MainSidebar(Request req)
        => [
            new LinkButton(CommonTexts.Account, "."),
            ..req.IsAdmin ? (IEnumerable<LinkButton>)[ new LinkButton(PluginTexts.ManageUsers, "users") ] : [],
            new LinkButton(CommonTexts.Logout, "logout"),
            new LinkButton(PluginTexts.LogoutOthers, "logout-others"),
            new LinkButton(CommonTexts.Settings, "settings")
        ];
    
    public static List<AbstractElement> SettingsSidebar
        => [
            new LinkButton(CommonTexts.Settings, "../settings"),
            new LinkButton(CommonTexts.Username, "username"),
            new LinkButton(CommonTexts.Mail, "email"),
            new LinkButton(CommonTexts.Password, "password"),
            new LinkButton(PluginTexts.TwoFactorLong, "2fa"),
            new LinkButton(PluginTexts.Applications, "apps"),
            new LinkButton(PluginTexts.DeleteAccount, "delete")
        ];
    
    public static List<AbstractElement> LoginSidebar(Request req)
        => [
            new LinkButton(CommonTexts.Login, $"login{req.CurrentRedirectQuery}"),
            new LinkButton(CommonTexts.Register, $"register{req.CurrentRedirectQuery}"),
            new LinkButton(PluginTexts.AccountRecovery, $"recovery{req.CurrentRedirectQuery}")
        ];
    
    public static List<AbstractElement> RecoverySidebar(Request req)
        => [
            new LinkButton(PluginTexts.AccountRecovery, $"../recovery{req.CurrentRedirectQuery}"),
            new LinkButton(CommonTexts.Username, $"username{req.CurrentRedirectQuery}"),
            new LinkButton(CommonTexts.Password, $"password{req.CurrentRedirectQuery}"),
            new LinkButton(PluginTexts.TwoFactorLong, $"2fa{req.CurrentRedirectQuery}")
        ];
}