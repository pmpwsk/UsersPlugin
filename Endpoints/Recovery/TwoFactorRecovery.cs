using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/recovery/2fa")]
    protected static IResponse HandleTwoFactorRecovery(Request req)
    {
        req.ForceGET();
        req.ForceNotBanned();
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
            PluginTexts.TwoFactorRecovery,
            RecoverySidebar(req),
            [
                new Section(
                    PluginTexts.TwoFactorRecovery,
                    [
                        new Subsection(
                            null,
                            [
                                new Paragraph(PluginTexts.TwoFactorRecoveryIntro),
                                Presets.CreateSupportButton(req)
                            ]
                        )
                    ]
                )
            ]
        );
    }
}