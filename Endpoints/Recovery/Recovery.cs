using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/recovery")]
    protected static IResponse HandleRecovery(Request req)
    {
        req.ForceGET();
        req.ForceNotBanned();
        switch (req.LoginState)
        {
            case LoginState.LoggedIn:
                return new RedirectResponse(req.RedirectUrl);
            case LoginState.Needs2FA:
                return new RedirectResponse("2fa" + req.CurrentRedirectQuery);
            case LoginState.NeedsMailVerification:
                return new RedirectResponse("verify" + req.CurrentRedirectQuery);
        }
        return new Page(
            req, false,
            PluginTexts.AccountRecovery,
            LoginSidebar(req),
            [
                new Section(
                    PluginTexts.AccountRecovery,
                    [
                        new Subsection(
                            null,
                            [
                                new Paragraph(PluginTexts.AccountRecoveryIntro),
                                new BigLinkButton(PluginTexts.UsernameRecovery, PluginTexts.UsernameRecoveryDescription, $"recovery/username{req.CurrentRedirectQuery}"),
                                new BigLinkButton(PluginTexts.PasswordRecovery, PluginTexts.PasswordRecoveryDescription, $"recovery/password{req.CurrentRedirectQuery}"),
                                new BigLinkButton(PluginTexts.TwoFactorRecovery, PluginTexts.TwoFactorRecoveryDescription, $"recovery/2fa{req.CurrentRedirectQuery}"),
                                Presets.CreateSupportButton(req)
                            ]
                        )
                    ]
                )
            ]
        );
    }
}