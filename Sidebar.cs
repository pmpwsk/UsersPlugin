using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    public static List<AbstractButton> MainSidebar(Request req)
        => [
            new LinkButton(new("bi bi-person", "Account"), "."),
            ..req.IsAdmin ? (IEnumerable<LinkButton>)[ new LinkButton(new("bi bi-people", "Manage users"), "users") ] : [],
            new LinkButton(new("bi bi-gear", "Settings"), "settings")
        ];
    
    public static List<AbstractButton> SettingsSidebar
        => [
            new LinkButton(new("bi bi-gear", "Settings"), "../settings"),
            new LinkButton(new("bi bi-person", "Username"), "username"),
            new LinkButton(new("bi bi-envelope", "Email address"), "email"),
            new LinkButton(new("bi bi-key", "Password"), "password"),
            new LinkButton(new("bi bi-lock", "Two-factor authentication"), "2fa"),
            new LinkButton(new("bi bi-hdd-rack", "Applications"), "apps"),
            new LinkButton(new("bi bi-trash", "Delete account"), "delete")
        ];
    
    public static List<AbstractButton> LoginSidebar(Request req)
        => [
            new LinkButton(new("bi bi-key", "Login"), $"login{req.CurrentRedirectQuery}"),
            new LinkButton(new("bi bi-person-vcard", "Register"), $"register{req.CurrentRedirectQuery}"),
            new LinkButton(new("bi bi-life-preserver", "Account recovery"), $"recovery{req.CurrentRedirectQuery}")
        ];
    
    public static List<AbstractButton> RecoverySidebar(Request req)
        => [
            new LinkButton(new("bi bi-life-preserver", "Account recovery"), $"../recovery{req.CurrentRedirectQuery}"),
            new LinkButton(new("bi bi-person", "Username"), $"username{req.CurrentRedirectQuery}"),
            new LinkButton(new("bi bi-key", "Password"), $"password{req.CurrentRedirectQuery}"),
            new LinkButton(new("bi bi-lock", "2FA"), $"2fa{req.CurrentRedirectQuery}")
        ];
}