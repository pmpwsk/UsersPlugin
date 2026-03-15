using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static async Task<IResponse> HandleSetPasswordRecovery(Request req)
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
        if (!req.Query.TryGetValue("token", out var token) || token.Length != 76)
            return StatusResponse.BadRequest;
        
        string id = token[..12];
        string code = token[12..];
        var user = await req.UserTable.GetByIdNullableAsync(id);
        if (user == null || user.Settings.TryGet("PasswordReset") != code)
            return new RedirectResponse("password");
        
        var page = new Page(req, true, "Password reset");
        page.Sections.Add(new(
            "Password reset",
            [
                new ServerForm(
                    null,
                    [
                        new Paragraph("Enter a new password and confirm it below."),
                        new Heading3("New password"),
                        new TextBox("password1", "Enter a password...", null, TextBoxRole.NewPassword) { Autofocus = true }
                            .Save(out var passwordInput1),
                        new Heading3("Confirm password"),
                        new TextBox("password1", "Enter the password again...", null, TextBoxRole.NewPassword)
                            .Save(out var passwordInput2),
                        new SubmitButton(new("bi bi-arrow-return-right", "Change"))
                    ],
                    async actionReq =>
                    {
                        if (!actionReq.HasUser)
                        {
                            if (passwordInput1.IsEmpty(out var password1) || passwordInput2.IsEmpty(out var password2) || password1 != password2)
                                return page.DynamicErrorAction("Please enter a new password twice.");

                            try
                            {
                                await actionReq.UserTable.SetPasswordAsync(user.Id, password1);
                                await Presets.WarningMailAsync(actionReq, user, "Password changed", "Your password was just changed by recovery.");
                                await actionReq.UserTable.DeleteSettingAsync(user.Id, "PasswordReset");
                            }
                            catch (Exception ex)
                            {
                                return page.DynamicErrorAction(ex.Message);
                            }
                        }

                        return new Navigate("../login" + req.CurrentRedirectQuery);
                    }
                )
            ]
        ));
        return page;
    }
}