using uwap.WebFramework.Accounts;
using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/2fa")]
    protected static IResponse HandleTwoFactor(Request req)
    {
        req.ForceGET();
        req.ForceNotBanned();
        switch (req.LoginState)
        {
            case LoginState.LoggedIn:
                return new RedirectResponse(req.RedirectUrl);
            case LoginState.NeedsMailVerification:
                return new RedirectResponse("verify" + req.CurrentRedirectQuery);
            case LoginState.None:
                return new RedirectResponse("login" + req.CurrentRedirectQuery);
            case LoginState.Needs2FA:
                break;
            default:
                return StatusResponse.Forbidden;
        }
        var page = new Page(req, true, CommonTexts.Login);
        page.Sections.Add(new(
            CommonTexts.Login,
            [
                new ServerForm(
                    null,
                    [
                        new Heading3(CommonTexts.TwoFactorOrRecovery),
                        new TextBox("code", CommonTexts.TwoFactorPlaceholder, null, TextBoxRole.NoSpellcheck) { Autofocus = true }
                            .Save(out var codeInput),
                        new ContinueButton()
                    ],
                    async actionReq =>
                    {
                        if (actionReq.LoginState == LoginState.Needs2FA && actionReq.User.TwoFactor.TOTPEnabled())
                        {
                            if (codeInput.IsEmpty(out var code))
                                throw new ForcedActionError(CommonTexts.EmptyFields);
                            
                            if (!await actionReq.UserTable.ValidateTOTPAsync(actionReq.User.Id, code, actionReq, true))
                                throw new ForcedActionError(PluginTexts.CodeInvalid);
                                
                            await Presets.WarningMailAsync(actionReq, actionReq.User, PluginTexts.NewLogin, PluginTexts.NewLoginMail);
                        }
                        
                        page.Navigate(req.RedirectUrl);
                    }
                ),
                new Subsection(
                    null,
                    [
                        new BigServerActionButton(
                            PluginTexts.LogOutInstead,
                            PluginTexts.LogInLater,
                            async actionReq =>
                            {
                                await actionReq.UserTable.LogoutAsync(actionReq);
                                page.Navigate(req.RedirectUrl);
                            }
                        )
                    ]
                )
            ]
        ));
        return page;
    }
}