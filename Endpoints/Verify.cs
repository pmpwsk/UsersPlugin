using uwap.WebFramework.Accounts;
using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/verify")]
    protected static IResponse HandleVerify(Request req)
    {
        req.ForceGET();
        req.ForceNotBanned();
        if (!req.HasUser)
            return StatusResponse.NotAuthenticated;
        switch (req.LoginState)
        {
            case LoginState.LoggedIn:
                return new RedirectResponse(req.RedirectUrl);
            case LoginState.Needs2FA:
                return new RedirectResponse("2fa" + req.CurrentRedirectQuery);
            case LoginState.None:
                return new RedirectResponse("login" + req.CurrentRedirectQuery);
            case LoginState.NeedsMailVerification:
                break;
            default:
                return StatusResponse.Forbidden;
        }
        var page = new Page(req, true, PluginTexts.Verify);
        page.Sections.Add(new(
            PluginTexts.Verify,
            [
                new Subsection(
                    null,
                    [
                        new Paragraph(PluginTexts.VerifyIntro),
                        new ServerActionButton(PluginTexts.Resend, async actionReq =>
                        {
                            if (actionReq.HasUser && actionReq.User.MailToken != null)
                                await Presets.WarningMailAsync(req, actionReq.User, PluginTexts.Welcome, $"{PluginTexts.WelcomeMail1} <a href=\"{req.ProtoHost}\">{req.Domain}</a>.\n{PluginTexts.WelcomeMail2} <a href=\"{req.PluginPathPrefix}/verify-link?user={req.User.Id}&code={req.User.MailToken}\">{PluginTexts.Here}</a> {PluginTexts.WelcomeMail3} {req.User.MailToken}");
                            
                            DialogBuilder.Info(page, PluginTexts.CodeSent);
                        })
                    ]
                ),
                new ServerForm(
                    null,
                    [
                        new Heading3(PluginTexts.MailVerificationCode),
                        new TextBox("code", PluginTexts.CodePlaceholder, null, TextBoxRole.NoSpellcheck)
                            .Save(out var codeInput),
                        new ContinueButton()
                    ],
                    async actionReq =>
                    {
                        if (codeInput.IsEmpty(out var code))
                            throw new ForcedActionError(CommonTexts.EmptyFields);

                        if (actionReq.HasUser && actionReq.User.MailToken != null
                            && !await req.UserTable.VerifyMailAsync(actionReq.User.Id, code, actionReq))
                            throw new ForcedActionError(PluginTexts.CodeInvalid);
                        
                        page.Navigate(req.RedirectUrl);
                    }
                ),
                new Subsection(
                    null,
                    [
                        new BigLinkButton(
                            PluginTexts.ChangeMail,
                            PluginTexts.ChangeMailDescription,
                            "verify-change"
                        ),
                        new BigServerActionButton(
                            PluginTexts.LogOutInstead,
                            PluginTexts.SetUpLater,
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