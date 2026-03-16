using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static Page HandleEmailSettings(Request req)
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
                                return new Reload();
                            }),
                            new ServerActionButton("Resend", async actionReq =>
                            {
                                actionReq.ForceLogin(false);
                                if (!actionReq.User.Settings.TryGetValue("EmailChange", out settingRaw))
                                    return new Reload();
                                setting = settingRaw.Split('&');
                                mail = HttpUtility.UrlDecode(setting[0]);
                                string existingCode = setting[1];
                                await Presets.WarningMailAsync(actionReq, actionReq.User, "Email change", $"You requested to change your email address to this address. Your verification code is: {existingCode}", mail);
                                return page.DynamicInfoAction("The code has been sent.");
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
                                return page.DynamicErrorAction("Please enter the verification code.");

                            if (!actionReq.User.Settings.TryGetValue("EmailChange", out settingRaw))
                                return new Reload();
                            setting = settingRaw.Split('&');
                            mail = HttpUtility.UrlDecode(setting[0]);
                            string existingCode = setting[1];
                            if (code != existingCode)
                            {
                                AccountManager.ReportFailedAuth(actionReq);
                                return page.DynamicErrorAction("The provided code is invalid.");
                            }
                            try
                            {
                                string oldMail = actionReq.User.MailAddress;
                                await actionReq.UserTable.SetMailAddressAsync(actionReq.User.Id, mail);
                                await Presets.WarningMailAsync(actionReq, actionReq.User, "Email changed", $"Your email was just changed to {mail}.", oldMail);
                                await actionReq.UserTable.DeleteSettingAsync(actionReq.User.Id, "EmailChange");
                                return new Navigate("../settings");
                            }
                            catch (Exception ex)
                            {
                                return page.DynamicErrorAction(ex.Message);
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
                            new SubmitButton(new("bi bi-arrow-return-right", "Continue"))
                        ],
                        async actionReq =>
                        {
                            actionReq.ForceLogin(false);
                            if (emailInput.IsEmpty(out var email) || auth.AnyEmpty)
                                return page.DynamicErrorAction("Please enter an email address and authenticate yourself.");

                            if (!await Presets.ValidateAuth(actionReq, auth))
                                return page.DynamicErrorAction($"The provided password{(auth.CodeInput != null ? " or 2FA code" : "")} is invalid.");

                            if (actionReq.User.MailAddress == email)
                                return page.DynamicErrorAction("The provided email address is the same as the old one.");

                            if (!AccountManager.CheckMailAddressFormat(email))
                                return page.DynamicErrorAction("The provided email address is invalid.");
                            if (await actionReq.UserTable.FindByMailAddressAsync(email) != null)
                                return page.DynamicErrorAction("This email address is already being used by another account.");

                            string code = Parsers.RandomString(10);
                            await actionReq.UserTable.SetSettingAsync(actionReq.User.Id, "EmailChange", $"{HttpUtility.UrlEncode(email)}&{code}");
                            await Presets.WarningMailAsync(actionReq, actionReq.User, "Email change", $"You requested to change your email address to this address. Your verification code is: {code}", email);
                            return new Navigate("email");
                        }
                    )
                ]
            ));
        }
        return page;
    }
}