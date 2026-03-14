using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static Page HandleUsernameSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true);
        page.Title = "Username settings";
        var usernameInput = new TextBox("username", "Enter a username...", null, TextBoxRole.Username) { Autofocus = true };
        var auth = Presets.CreateAuthElements(req);
        page.Sections.Add(new(
            "Username settings",
            [
                new ServerForm(
                    null,
                    async actionReq =>
                    {
                        actionReq.ForceLogin(false);
                        if (usernameInput.IsEmpty(out var username) || auth.AnyEmpty)
                            return page.DynamicErrorAction("Please enter a username and authenticate yourself.");
    
                        if (!await Presets.ValidateAuth(actionReq, auth))
                            return page.DynamicErrorAction($"The provided password{(auth.CodeInput != null ? " or 2FA code" : "")} is invalid.");

                        try
                        {
                            await actionReq.UserTable.SetUsernameAsync(actionReq.User.Id, username);
                            await Presets.WarningMailAsync(actionReq, actionReq.User, "Username changed", $"Your username was just changed to {username}.");
                            return new Navigate("../settings");
                        }
                        catch (Exception ex)
                        {
                            return page.DynamicErrorAction(ex.Message);
                        }
                    },
                    [
                        new Paragraph("Warning: Other devices will remain logged in."),
                        new Paragraph("Current: " + req.User.Username),
                        new Heading3("New username"),
                        usernameInput,
                        ..auth.Elements,
                        new SubmitButton(new("bi bi-arrow-return-right", "Change"))
                    ]
                )
            ]
        ));
        return page;
    }
}