using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/recovery/username")]
    protected static IResponse HandleUsernameRecovery(Request req)
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
        var page = new Page(req, true, "Username recovery");
        page.Sidebar.Items.ReplaceAll(RecoverySidebar(req));
        page.Sections.Add(new(
            "Username recovery",
            [
                new ServerForm(
                    null,
                    [
                        new Paragraph("Enter your email address below and you'll receive an email telling you your username."),
                        new Heading3("Email"),
                        new TextBox("email", "Enter your email address...", null, TextBoxRole.Email) { Autofocus = true }
                            .Save(out var emailInput),
                        new ContinueButton()
                    ],
                    async actionReq =>
                    {
                        if (!actionReq.HasUser)
                        {
                            if (emailInput.IsEmpty(out var email))
                            {
                                DialogBuilder.Error(page, "Please enter your email address.");
                                return;
                            }
    
                            User? user = await actionReq.UserTable.FindByMailAddressAsync(email);
                            if (user == null)
                            {
                                AccountManager.ReportFailedAuth(actionReq);
                                DialogBuilder.Error(page, "This email address isn't associated with any account.");
                                return;
                            }
                            await Presets.WarningMailAsync(actionReq, user, "Username recovery", $"You requested your username, it is: {user.Username}");
                        }

                        page.Navigate("../login" + req.CurrentRedirectQuery);
                    }
                )
            ]
        ));
        return page;
    }
}