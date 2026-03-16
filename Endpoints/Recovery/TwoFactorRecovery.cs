using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static IResponse HandleTwoFactorRecovery(Request req)
    {
        req.ForceGET();
        switch (req.LoginState)
        {
            case LoginState.LoggedIn:
                return new RedirectResponse(req.RedirectUrl);
            case LoginState.Needs2FA:
                return new RedirectResponse("../2fa" + req.CurrentRedirectQuery);
            case LoginState.NeedsMailVerification:
                return new RedirectResponse("../verify" + req.CurrentRedirectQuery);
        }
        return new Page(
            req, false,
            "2FA recovery",
            RecoverySidebar(req),
            [
                new Section(
                    "2FA recovery",
                    [
                        new Subsection(
                            null,
                            [
                                new Paragraph("If you've lost access to your 2FA app, you can use one of the recovery codes you were given when you enabled 2FA. If you've lost those as well, please contact our support."),
                                Presets.CreateSupportButton(req)
                            ]
                        )
                    ]
                )
            ]
        );
    }
}