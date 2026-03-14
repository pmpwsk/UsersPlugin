using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static Page HandleDeleteSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true);
        page.Title = "Delete account";
        var auth = Presets.CreateAuthElements(req);
        page.Sections.Add(new(
            "Delete account",
            [
                new ServerForm(
                    null,
                    async actionReq =>
                    {
                        actionReq.ForceLogin(false);
                        if (auth.AnyEmpty)
                            return page.DynamicErrorAction("Please authenticate yourself.");
    
                        if (!await Presets.ValidateAuth(actionReq, auth))
                            return page.DynamicErrorAction($"The provided password{(auth.CodeInput != null ? " or 2FA code" : "")} is invalid.");

                        actionReq.CookieWriter?.Delete("AuthToken");
                        await actionReq.UserTable.DeleteAllTokensAsync(actionReq.User.Id);
                        await actionReq.UserTable.SetSettingAsync(actionReq.User.Id, "Delete", DateTime.UtcNow.Ticks.ToString());
                        await Presets.WarningMailAsync(actionReq, actionReq.User, "Account deletion", "You just requested your account to be deleted. We will keep your data for another 30 days, in case you change your mind. If you want to restore your account, simply log in again within the next 30 days. If you want us to delete your data immediately, please contact us by replying to this email.");
                        return new Navigate("/");
                    },
                    [
                        new Paragraph("We're very sad to see you go! If you're leaving because you've been experiencing issues, please let us know and we'll try our best to fix it. The goal of this project is to make your experience as nice as possible."),
                        new Paragraph($"If you really want to delete your account, enter your password{(req.User.TwoFactor.TOTPEnabled()?" and 2FA code":"")} below."),
                        ..auth.Elements,
                        new SubmitButton(new("bi bi-emoji-frown", "Delete"))
                    ]
                )
            ]
        ));
        return page;
    }
}