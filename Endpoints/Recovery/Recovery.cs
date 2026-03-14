using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static IResponse HandleRecovery(Request req)
    {
        req.ForceGET();
        switch (req.LoginState)
        {
            case LoginState.LoggedIn:
                return new RedirectResponse(req.RedirectUrl);
            case LoginState.Needs2FA:
                return new RedirectResponse("2fa" + req.CurrentRedirectQuery);
            case LoginState.NeedsMailVerification:
                return new RedirectResponse("verify" + req.CurrentRedirectQuery);
        }
        var page = new Page(req, false);
        page.Title = "Recovery";
        page.Sections.Add(new(
            "Recovery",
            [
                new Subsection(
                    null,
                    [
                        new Paragraph("Here are some options to recover your account. If you still can't log in, please contact our support."),
                        new BigLinkButton(new("bi bi-person", "Username"), ["Receive an email containing your username."], $"recovery/username{req.CurrentRedirectQuery}"),
                        new BigLinkButton(new("bi bi-key", "Password"), ["Receive an email to reset your password."], $"recovery/password{req.CurrentRedirectQuery}"),
                        new BigLinkButton(new("bi bi-lock", "Two-factor authentication"), ["Information about 2FA recovery."], $"recovery/2fa{req.CurrentRedirectQuery}"),
                        Presets.CreateSupportButton(req)
                    ]
                )
            ]
        ));
        return page;
    }
}