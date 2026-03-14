using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static IResponse HandlePasswordRecovery(Request req)
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
        var page = new Page(req, true);
        page.Title = "Password recovery";
        var emailInput = new TextBox("email", "Enter your email address...", null, TextBoxRole.Email) { Autofocus = true };
        page.Sections.Add(new(
            "Password recovery",
            [
                new ServerForm(
                    null,
                    async actionReq =>
                    {
                        if (!actionReq.HasUser)
                        {
                            if (emailInput.IsEmpty(out var email))
                                return page.DynamicErrorAction("Please enter your email address.");

                            User? user = await actionReq.UserTable.FindByMailAddressAsync(email);
                            if (user == null)
                            {
                                AccountManager.ReportFailedAuth(actionReq);
                                return page.DynamicErrorAction("This email address isn't associated with any account.");
                            }
                            string code = Parsers.RandomString(64);
                            await actionReq.UserTable.SetSettingAsync(user.Id, "PasswordReset", code);
                            string url = $"{req.PluginPathPrefix}/recovery/password-set?token={user.Id}{code}";
                            await Presets.WarningMailAsync(actionReq, user, "Password recovery", $"You requested password recovery. Open the following link to reset your password:\n<a href=\"{url}\">{url}</a>\nYou can cancel the password reset in the account panel.");
                        }

                        return new Navigate("../login" + req.CurrentRedirectQuery);
                    },
                    [
                        new Paragraph("Enter your email address below and you'll receive an email with a link to reset your password."),
                        new Heading3("Email"),
                        emailInput,
                        new SubmitButton(new("bi bi-arrow-return-right", "Continue"))
                    ]
                )
            ]
        ));
        return page;
    }
}