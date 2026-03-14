using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static IResponse HandleVerify(Request req)
    {
        req.ForceGET();
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
        var page = new Page(req, true);
        page.Title = "Verify";
        var codeInput = new TextBox("code", "Enter the code...", null, TextBoxRole.NoSpellcheck);
        page.Sections.Add(new(
            "Verify",
            [
                new Subsection(
                    null,
                    [
                        new Paragraph("You should have received an email containing a verification code, please enter it below to finish setting up your account."),
                        new ServerActionButton(new("bi bi-envelope", "Send again"), async actionReq =>
                        {
                            if (actionReq.HasUser && actionReq.User.MailToken != null)
                                await Presets.WarningMailAsync(req, actionReq.User, "Welcome", $"Thank you for registering on <a href=\"{req.ProtoHost}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.PluginPathPrefix}/verify-link?user={req.User.Id}&code={req.User.MailToken}\">here</a> or enter the following code: {req.User.MailToken}");
                            
                            return page.DynamicInfoAction("The code has been sent.");
                        })
                    ]
                ),
                new ServerForm(
                    null,
                    async actionReq =>
                    {
                        if (codeInput.IsEmpty(out var code))
                            return page.DynamicErrorAction("Please enter the verification code.");
                        
                        if (!actionReq.HasUser || actionReq.User.MailToken == null
                            || await req.UserTable.VerifyMailAsync(actionReq.User.Id, code, actionReq))
                            return new Navigate(req.RedirectUrl);
                        else
                            return page.DynamicErrorAction("The provided code is invalid.");
                    },
                    [
                        new Heading3("Verification code"),
                        codeInput,
                        new SubmitButton(new("bi bi-arrow-return-right", "Verify"))
                    ]
                ),
                new Subsection(
                    null,
                    [
                        new BigLinkButton(new("bi bi-arrow-left-right", "Change email address"), ["Try another email address."], "verify-change"),
                        new BigLinkButton(new("bi bi-box-arrow-left", "Log out instead"), ["Set up your account later."], $"logout{req.CurrentRedirectQuery}")
                    ]
                )
            ]
        ));
        return page;
    }
}