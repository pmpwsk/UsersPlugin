using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static IResponse HandleTwoFactor(Request req)
    {
        req.ForceGET();
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
        var page = new Page(req, true, "2FA");
        page.Sections.Add(new(
            "2FA",
            [
                new ServerForm(
                    null,
                    [
                        new Heading3("2FA code / recovery"),
                        new TextBox("code", "Enter the current code...", null, TextBoxRole.NoSpellcheck) { Autofocus = true }
                            .Save(out var codeInput),
                        new SubmitButton(new("bi bi-arrow-return-right", "Continue"))
                    ],
                    async actionReq =>
                    {
                        if (actionReq.LoginState == LoginState.Needs2FA && actionReq.User.TwoFactor.TOTPEnabled())
                        {
                            if (codeInput.IsEmpty(out var code))
                                return page.DynamicErrorAction("Please enter the current code or a recovery code.");
                            
                            if (!await actionReq.UserTable.ValidateTOTPAsync(actionReq.User.Id, code, actionReq, true))
                                return page.DynamicErrorAction("The provided code is invalid.");
                                
                            await Presets.WarningMailAsync(actionReq, actionReq.User, "New login", "Someone just successfully logged into your account.");
                        }
                        
                        return new Navigate(req.RedirectUrl);
                    }
                ),
                new Subsection(
                    null,
                    [
                        new BigLinkButton(new("bi bi-box-arrow-left", "Log out instead"), ["Set up your account later."], $"logout{req.CurrentRedirectQuery}")
                    ]
                )
            ]
        ));
        return page;
    }
}