using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static Page HandlePasswordSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true);
        page.Title = "Password settings";
        var passwordInput1 = new TextBox("password1", "Enter a password...", null, TextBoxRole.NewPassword) { Autofocus = true };
        var passwordInput2 = new TextBox("password2", "Enter a password...", null, TextBoxRole.NewPassword);
        var auth = Presets.CreateAuthElements(req);
        page.Sections.Add(new(
            "Password settings",
            [
                new ServerForm(
                    null,
                    async actionReq =>
                    {
                        actionReq.ForceLogin(false);
                        if (passwordInput1.IsEmpty(out var password1) || passwordInput2.IsEmpty(out var password2) || auth.AnyEmpty)
                            return page.DynamicErrorAction("Please enter a new password twice and authenticate yourself.");
                        if (password1 != password2)
                            return page.DynamicErrorAction("The passwords do not match.");

                        if (!await Presets.ValidateAuth(actionReq, auth))
                            return page.DynamicErrorAction($"The provided password{(auth.CodeInput != null ? " or 2FA code" : "")} is invalid.");

                        try
                        {
                            await req.UserTable.SetPasswordAsync(req.User.Id, password1);
                            await Presets.WarningMailAsync(req, req.User, "Password changed", "Your password was just changed.");
                            return new Navigate("../settings");
                        }
                        catch (Exception ex)
                        {
                            return page.DynamicErrorAction(ex.Message);
                        }
                    },
                    [
                        new Paragraph("Warning: Other devices will remain logged in."),
                        new Heading3("New password"),
                        passwordInput1,
                        new Heading3("Confirm password"),
                        passwordInput2,
                        ..auth.Elements,
                        new SubmitButton(new("bi bi-arrow-return-right", "Continue"))
                    ]
                )
            ]
        ));
        return page;
    }
}