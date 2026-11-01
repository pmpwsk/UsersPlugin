using uwap.WebFramework.Accounts;
using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/recovery/password-set")]
    protected static async Task<IResponse> HandleSetPasswordRecovery(Request req)
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
        if (!req.Query.TryGetValue("token", out var token) || token.Length != 76)
            return StatusResponse.BadRequest;
        
        string id = token[..12];
        string code = token[12..];
        var user = await req.UserTable.GetByIdNullableAsync(id);
        if (user == null || user.Settings.TryGet("PasswordReset") != code)
            return new RedirectResponse("password");
        
        var page = new Page(req, true, PluginTexts.PasswordRecovery);
        page.Sections.Add(new(
            PluginTexts.PasswordRecovery,
            [
                new ServerForm(
                    null,
                    [
                        new Paragraph(PluginTexts.PasswordRecoverySetIntro),
                        new Heading3(PluginTexts.NewPassword),
                        new TextBox("password1", PluginTexts.NewPasswordPlaceholder, null, TextBoxRole.NewPassword, true)
                            .Save(out var passwordInput1),
                        new Heading3(PluginTexts.NewPasswordAgain),
                        new TextBox("password2", PluginTexts.NewPasswordAgainPlaceholder, null, TextBoxRole.NewPassword)
                            .Save(out var passwordInput2),
                        new ContinueButton()
                    ],
                    async actionReq =>
                    {
                        if (!actionReq.HasUser)
                        {
                            if (passwordInput1.IsEmpty(out var password1) || passwordInput2.IsEmpty(out var password2))
                                throw new ForcedActionError(CommonTexts.EmptyFields);
                        
                            if (password1 != password2)
                                throw new ForcedActionError(PluginTexts.NewPasswordsUnequal);

                            try
                            {
                                await actionReq.UserTable.SetPasswordAsync(user.Id, password1);
                                await Presets.WarningMailAsync(actionReq, user, PluginTexts.PasswordChanged, PluginTexts.PasswordRecoveredMail);
                                await actionReq.UserTable.DeleteSettingAsync(user.Id, "PasswordReset");
                            }
                            catch (Exception ex)
                            {
                                throw new ForcedActionError(ex.Message);
                            }
                        }

                        await page.NavigateAsync("../login" + req.CurrentRedirectQuery);
                    }
                )
            ]
        ));
        return page;
    }
}