using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static IResponse HandleVerifyChange(Request req)
    {
        req.ForceGET();
        switch (req.LoginState)
        {
            case LoginState.LoggedIn:
                return new RedirectResponse(req.RedirectUrl);
            case LoginState.Needs2FA:
                return new RedirectResponse("../2fa" + req.CurrentRedirectQuery);
            case LoginState.None:
                return new RedirectResponse("../login" + req.CurrentRedirectQuery);
            case LoginState.NeedsMailVerification:
                break;
            default:
                return StatusResponse.Forbidden;
        }
        var page = new Page(req, true, "Change email");
        page.Sections.Add(new(
            "Change email",
            [
                new ServerForm(
                    null,
                    [
                        new Heading3("Email"),
                        new TextBox("email", "Enter your email address...", null, TextBoxRole.Email)
                            .Save(out var emailInput),
                        new SubmitButton(new("bi bi-arrow-return-right", "Change"))
                    ],
                    async actionReq =>
                    {
                        if (actionReq.HasUser && actionReq.User.MailToken != null)
                        {
                            if (emailInput.IsEmpty(out var email))
                                return DialogBuilder.DynamicErrorAction(page, "Please enter your email address.");

                            try
                            {
                                await actionReq.UserTable.SetMailAddressAsync(actionReq.User.Id, email);
                                var user = await actionReq.UserTable.SetNewMailTokenAsync(actionReq.User.Id);
                                await Presets.WarningMailAsync(req, user, "Welcome", $"Thank you for registering on <a href=\"{req.ProtoHost}\">{req.Domain}</a>.\nTo verify your email address, click <a href=\"{req.PluginPathPrefix}/verify-link?user={user.Id}&code={user.MailToken}\">here</a> or enter the following code: {user.MailToken}");
                            }
                            catch (Exception ex)
                            {
                                return DialogBuilder.DynamicErrorAction(page, ex.Message);
                            }
                        }
                        
                        return new Navigate("verify");
                    }
                )
            ]
        ));
        return page;
    }
}