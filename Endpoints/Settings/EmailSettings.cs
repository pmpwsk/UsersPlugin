using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/settings/email")]
    protected static Page HandleEmailSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true, "Email settings");
        page.Sidebar.Items.ReplaceAll(SettingsSidebar);
        if (req.User.Settings.TryGetValue("EmailChange", out var settingRaw))
        {
            string[] setting = settingRaw.Split('&');
            string mail = HttpUtility.UrlDecode(setting[0]);
            page.Sections.Add(new(
                "Email settings",
                [
                    new Subsection(
                        null,
                        [
                            new Paragraph($"You requested to change your email to '{mail}'. Please enter the verification code provided in the email we sent to that address here."),
                            new ServerActionButton("Cancel", async actionReq =>
                            {
                                actionReq.ForceLogin(false);
                                await req.UserTable.DeleteSettingAsync(actionReq.User.Id, "EmailChange");
                                page.Reload();
                            }),
                            new ServerActionButton("Resend", async actionReq =>
                            {
                                actionReq.ForceLogin(false);
                                if (!actionReq.User.Settings.TryGetValue("EmailChange", out settingRaw))
                                {
                                    page.Reload();
                                    return;
                                }
                                setting = settingRaw.Split('&');
                                mail = HttpUtility.UrlDecode(setting[0]);
                                string existingCode = setting[1];
                                await Presets.WarningMailAsync(actionReq, actionReq.User, "Email change", $"You requested to change your email address to this address. Your verification code is: {existingCode}", mail);
                                DialogBuilder.Info(page, "The code has been sent.");
                            })
                        ]
                    ),
                    new ServerForm(
                        null,
                        [
                            new Heading3("Verification code"),
                            new TextBox("code", "Enter the code...", null, TextBoxRole.NoSpellcheck) { Autofocus = true }
                                .Save(out var codeInput),
                            new SubmitButton("Change")
                        ],
                        async actionReq =>
                        {
                            actionReq.ForceLogin(false);
                            if (codeInput.IsEmpty(out var code))
                            {
                                DialogBuilder.Error(page, "Please enter the verification code.");
                                return;
                            }

                            if (!actionReq.User.Settings.TryGetValue("EmailChange", out settingRaw))
                            {
                                page.Reload();
                                return;
                            }
                            setting = settingRaw.Split('&');
                            mail = HttpUtility.UrlDecode(setting[0]);
                            string existingCode = setting[1];
                            if (code != existingCode)
                            {
                                AccountManager.ReportFailedAuth(actionReq);
                                DialogBuilder.Error(page, "The provided code is invalid.");
                                return;
                            }
                            
                            try
                            {
                                string oldMail = actionReq.User.MailAddress;
                                await actionReq.UserTable.SetMailAddressAsync(actionReq.User.Id, mail);
                                await Presets.WarningMailAsync(actionReq, actionReq.User, "Email changed", $"Your email was just changed to {mail}.", oldMail);
                                await actionReq.UserTable.DeleteSettingAsync(actionReq.User.Id, "EmailChange");
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
        }
        else
        {
            page.Sections.Add(new(
                "Email settings",
                [
                    new ServerForm(
                        null,
                        [
                            new Paragraph($"Current: {req.User.MailAddress}"),
                            new Heading3("New email"),
                            new TextBox("email", "Enter your email address...", null, TextBoxRole.Email) { Autofocus = true }
                                .Save(out var emailInput),
                            ..Presets.CreateAuthElements(req)
                                .Save(out var auth).Elements,
                            new ContinueButton()
                        ],
                        async actionReq =>
                        {
                            actionReq.ForceLogin(false);
                            if (emailInput.IsEmpty(out var email) || auth.AnyEmpty)
                                DialogBuilder.Error(page, "Please enter an email address and authenticate yourself.");
                            else if (!await Presets.ValidateAuth(actionReq, auth))
                                DialogBuilder.Error(page, $"The provided password{(auth.CodeInput != null ? " or 2FA code" : "")} is invalid.");
                            else if (actionReq.User.MailAddress == email)
                                DialogBuilder.Error(page, "The provided email address is the same as the old one.");
                            else if (!AccountManager.CheckMailAddressFormat(email))
                                DialogBuilder.Error(page, "The provided email address is invalid.");
                            else if (await actionReq.UserTable.FindByMailAddressAsync(email) != null)
                                DialogBuilder.Error(page, "This email address is already being used by another account.");
                            else
                            {
                                string code = Parsers.RandomString(10);
                                await actionReq.UserTable.SetSettingAsync(actionReq.User.Id, "EmailChange", $"{HttpUtility.UrlEncode(email)}&{code}");
                                await Presets.WarningMailAsync(actionReq, actionReq.User, "Email change", $"You requested to change your email address to this address. Your verification code is: {code}", email);
                                page.Navigate("email");
                            }
                        }
                    )
                ]
            ));
        }
        return page;
    }
}