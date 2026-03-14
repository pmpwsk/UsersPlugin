using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static Page HandleSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, false);
        page.Title = "Settings";
        page.Sections.Add(new Section(
            "Settings",
            [
                new Subsection(
                    null,
                    [
                        new BigLinkButton(new("bi bi-eye", "Theme"), [ "Adjust the UI theme." ], "settings/theme"),
                        new BigLinkButton(new("bi bi-person", "Username"), [ "Change your username." ], "settings/username"),
                        new BigLinkButton(new("bi bi-envelope", "Email address"), [ "Change your email address." ], "settings/email"),
                        new BigLinkButton(new("bi bi-key", "Password"), [ "Change your password." ], "settings/password"),
                        new BigLinkButton(new("bi bi-lock", "Two-factor authentication"), [ $"{(req.User.TwoFactor.TOTPEnabled() ? "Disable" : "Enable")} 2FA." ], "settings/2fa"),
                        new BigLinkButton(new("bi bi-hdd-rack", "Applications"), [ "Manage apps that can access your account." ], "settings/apps"),
                        new BigLinkButton(new("bi bi-trash", "Delete account"), [ "Close your account." ], "settings/delete")
                    ]
                )
        ]));
        return page;
    }
}