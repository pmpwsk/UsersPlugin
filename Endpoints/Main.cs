using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static Page HandleMenu(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true);
        page.Title = "Account";
        Section? section = null;
        Subsection? passwordResetSubsection = null;
        passwordResetSubsection = new Subsection(
            "Alert",
            [
                new Paragraph("A password reset has been requested and a corresponding link has been sent to your email address."),
                new ServerActionButton(new("bi bi-x-circle", "Cancel"), async _ =>
                {
                    await req.UserTable.DeleteSettingAsync(req.User.Id, "PasswordReset");
                    if (section != null && passwordResetSubsection != null)
                        section.Subsections.Remove(passwordResetSubsection);
                            
                    return new Nothing();
                })
            ]
        );
        var subsection = new Subsection(null);
        section = new Section("Account", req.User.Settings.ContainsKey("PasswordReset") ? [ passwordResetSubsection, subsection ] : [ subsection ]);
        page.Sections.Add(section);
        if (req.IsAdmin)
            subsection.Content.Add(new BigLinkButton(new("bi bi-people", "Manage users"), [ "Control all existing users." ], "users"));
        subsection.Content.Add(new BigLinkButton(new("bi bi-box-arrow-left", "Log out"), [ "Other devices will stay logged in." ], "logout"));
        subsection.Content.Add(new BigLinkButton(new("bi bi-slash-circle", "Log out all other devices"), [ "This device will stay logged in." ], "logout-others"));
        subsection.Content.Add(new BigLinkButton(new("bi bi-gear", "Settings"), [ "Control your account details." ], "settings"));
        return page;
    }
}