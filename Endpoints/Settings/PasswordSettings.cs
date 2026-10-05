using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/settings/password")]
    protected static Page HandlePasswordSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true, "Password settings");
        page.Sidebar.Items.ReplaceAll(SettingsSidebar);
        page.Sections.Add(new(
            "Password settings",
            [
                new ServerForm(
                    null,
                    [
                        new Paragraph("Warning: Other devices will remain logged in."),
                        new Heading3("New password"),
                        new TextBox("password1", "Enter a password...", null, TextBoxRole.NewPassword) { Autofocus = true }
                            .Save(out var passwordInput1),
                        new Heading3("Confirm password"),
                        new TextBox("password2", "Enter a password...", null, TextBoxRole.NewPassword)
                            .Save(out var passwordInput2),
                        ..Presets.CreateAuthElements(req)
                            .Save(out var auth).Elements,
                        new ContinueButton()
                    ],
                    async actionReq =>
                    {
                        actionReq.ForceLogin(false);
                        if (passwordInput1.IsEmpty(out var password1) || passwordInput2.IsEmpty(out var password2) || auth.AnyEmpty)
                            DialogBuilder.Error(page, "Please enter a new password twice and authenticate yourself.");
                        else if (password1 != password2)
                            DialogBuilder.Error(page, "The passwords do not match.");
                        else if (!await Presets.ValidateAuth(actionReq, auth))
                            DialogBuilder.Error(page, $"The provided password{(auth.CodeInput != null ? " or 2FA code" : "")} is invalid.");
                        else
                            try
                            {
                                await req.UserTable.SetPasswordAsync(req.User.Id, password1);
                                await Presets.WarningMailAsync(req, req.User, "Password changed", "Your password was just changed.");
                                page.Navigate("../settings");
                            }
                            catch (Exception ex)
                            {
                                DialogBuilder.Error(page, ex.Message);
                            }
                    }
                )
            ]
        ));
        return page;
    }
}