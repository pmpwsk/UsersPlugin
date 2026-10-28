using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/settings/email")]
    protected static Page HandleEmailSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true, CommonTexts.Mail);
        page.Sidebar.Items.ReplaceAll(SettingsSidebar);
        if (req.User.Settings.TryGetValue("EmailChange", out var settingRaw))
        {
            string[] setting = settingRaw.Split('&');
            string mail = HttpUtility.UrlDecode(setting[0]);
            page.Sections.Add(new(
                CommonTexts.Mail,
                [
                    new Subsection(
                        null,
                        [
                            new Paragraph($"{PluginTexts.MailChangeRequested1}{mail}{PluginTexts.MailChangeRequested2}"),
                            new ServerActionButton(CommonTexts.Cancel, async actionReq =>
                            {
                                if (!actionReq.LoggedIn)
                                    throw new ForcedActionReload();
                                await req.UserTable.DeleteSettingAsync(actionReq.User.Id, "EmailChange");
                                page.Reload();
                            }),
                            new ServerActionButton(PluginTexts.Resend, async actionReq =>
                            {
                                if (!actionReq.LoggedIn || !actionReq.User.Settings.TryGetValue("EmailChange", out settingRaw))
                                    throw new ForcedActionReload();

                                setting = settingRaw.Split('&');
                                mail = HttpUtility.UrlDecode(setting[0]);
                                string existingCode = setting[1];
                                await Presets.WarningMailAsync(actionReq, actionReq.User, PluginTexts.MailChange, $"{PluginTexts.MailChangeMail} {existingCode}", mail);
                                DialogBuilder.Info(page, PluginTexts.CodeSent);
                            })
                        ]
                    ),
                    new ServerForm(
                        null,
                        [
                            new Heading3(PluginTexts.MailVerificationCode),
                            new TextBox("code", PluginTexts.CodePlaceholder, null, TextBoxRole.NoSpellcheck) { Autofocus = true }
                                .Save(out var codeInput),
                            new ContinueButton()
                        ],
                        async actionReq =>
                        {
                            if (!actionReq.LoggedIn)
                                throw new ForcedActionReload();
                            
                            if (codeInput.IsEmpty(out var code))
                                throw new ForcedActionError(CommonTexts.EmptyFields);

                            if (!actionReq.User.Settings.TryGetValue("EmailChange", out settingRaw))
                                throw new ForcedActionReload();
                            
                            setting = settingRaw.Split('&');
                            mail = HttpUtility.UrlDecode(setting[0]);
                            string existingCode = setting[1];
                            if (code != existingCode)
                            {
                                AccountManager.ReportFailedAuth(actionReq);
                                throw new ForcedActionError(PluginTexts.CodeInvalid);
                            }
                            
                            try
                            {
                                string oldMail = actionReq.User.MailAddress;
                                await actionReq.UserTable.SetMailAddressAsync(actionReq.User.Id, mail);
                                await Presets.WarningMailAsync(actionReq, actionReq.User, PluginTexts.MailChange, $"{PluginTexts.MailChangedMail1}{mail}{PluginTexts.MailChangedMail2}", oldMail);
                                await actionReq.UserTable.DeleteSettingAsync(actionReq.User.Id, "EmailChange");
                                page.Navigate("../settings");
                            }
                            catch (Exception ex)
                            {
                                throw new ForcedActionError(ex.Message);
                            }
                        }
                    )
                ]
            ));
        }
        else
        {
            page.Sections.Add(new(
                CommonTexts.Mail,
                [
                    new ServerForm(
                        null,
                        [
                            new Paragraph($"{CommonTexts.Current}: {req.User.MailAddress}"),
                            new Heading3(PluginTexts.NewMail),
                            new TextBox("email", PluginTexts.NewMailPlaceholder, null, TextBoxRole.Email) { Autofocus = true }
                                .Save(out var emailInput),
                            ..Presets.CreateAuthElements(req)
                                .Save(out var auth).Elements,
                            new ContinueButton()
                        ],
                        async actionReq =>
                        {
                            if (!actionReq.LoggedIn)
                                throw new ForcedActionReload();
                            
                            if (emailInput.IsEmpty(out var email) || auth.AnyEmpty)
                                throw new ForcedActionError(CommonTexts.EmptyFields);
                            
                            if (!await Presets.ValidateAuth(actionReq, auth))
                                throw new ForcedActionError(auth.CodeInput != null ? PluginTexts.WrongAuthWith2FA : PluginTexts.WrongAuthWithout2FA);
                            
                            if (actionReq.User.MailAddress == email)
                                throw new ForcedActionError(CommonTexts.MailSame);
                            
                            if (!AccountManager.CheckMailAddressFormat(email))
                                throw new ForcedActionError(CommonTexts.MailFormat);
                            
                            if (await actionReq.UserTable.FindByMailAddressAsync(email) != null)
                                throw new ForcedActionError(CommonTexts.MailExists);
                            
                            string code = Parsers.RandomString(10);
                            await actionReq.UserTable.SetSettingAsync(actionReq.User.Id, "EmailChange", $"{HttpUtility.UrlEncode(email)}&{code}");
                            await Presets.WarningMailAsync(actionReq, actionReq.User, PluginTexts.MailChange, $"{PluginTexts.MailChangeMail} {code}", email);
                            page.Navigate("email");
                        }
                    )
                ]
            ));
        }
        return page;
    }
}