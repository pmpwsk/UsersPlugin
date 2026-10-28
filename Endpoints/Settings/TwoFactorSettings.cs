using System.Text;
using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/settings/2fa")]
    protected static async Task<Page> HandleTwoFactorSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        if (req.User.TwoFactor.TOTPEnabled())
        { //2fa enabled and verified, show option to disable it
            var page = new Page(req, true, PluginTexts.DisableTwoFactor);
            page.Sidebar.Items.ReplaceAll(SettingsSidebar);
            page.Sections.Add(new(
                PluginTexts.DisableTwoFactor,
                [
                    new ServerForm(
                        null,
                        [
                            new Paragraph(PluginTexts.DisableTwoFactorIntro),
                            new Paragraph(PluginTexts.WarningOthersStayLoggedIn),
                            ..Presets.CreateAuthElements(req)
                                .Save(out var auth).Elements,
                            new ContinueButton()
                        ],
                        async actionReq =>
                        {
                            if (!actionReq.LoggedIn)
                                throw new ForcedActionReload();
                            
                            if (auth.AnyEmpty)
                                throw new ForcedActionError(CommonTexts.EmptyFields);
                            
                            if (req.User.TwoFactor.TOTP == null || !req.User.TwoFactor.TOTP.Verified)
                                throw new ForcedActionReload();
                            
                            if (!await Presets.ValidateAuth(actionReq, auth))
                                throw new ForcedActionError(auth.CodeInput != null ? PluginTexts.WrongAuthWith2FA : PluginTexts.WrongAuthWithout2FA);
                            
                            await req.UserTable.DisableTOTPAsync(actionReq.User.Id);
                            await Presets.WarningMailAsync(actionReq, actionReq.User, PluginTexts.TwoFactorDisabled, PluginTexts.TwoFactorDisabledMail);
                            page.Navigate("../settings");
                        }
                    )
                ]
            ));
            return page;
        }
        else
        { //2fa not fully enabled, show option to enable it
            var page = new Page(req, true, PluginTexts.EnableTwoFactor);
            page.Sidebar.Items.ReplaceAll(SettingsSidebar);
            var totp = await req.UserTable.GenerateTOTPAsync(req.User.Id);
            page.Sections.Add(new(
                PluginTexts.EnableTwoFactor,
                [
                    new Subsection(
                        null,
                        [
                            new Paragraph(PluginTexts.EnableTwoFactorIntro),
                            new Paragraph(PluginTexts.WarningOthersStayLoggedIn)
                        ]
                    ),
                    new Subsection(
                        PluginTexts.PrivateKey,
                        [
                            new Paragraph(PluginTexts.PrivateKeyIntro),
                            new Image(req, totp.QRImageBase64Src(req.Domain, req.User.Username)) { Style = "max-height: 15rem" },
                            new Paragraph(totp.SecretKeyString)
                        ]
                    ),
                    new Subsection(
                        PluginTexts.RecoveryCodes,
                        [
                            new Paragraph(PluginTexts.RecoveryCodesIntro),
                            new BulletList(totp.RecoveryCodes.Select(c => new ListItem(c))),
                            new LinkButton(CommonTexts.Download, "2fa-codes") { NewTab = true }
                        ]
                    ),
                    new ServerForm(
                        PluginTexts.Confirmation,
                        [
                            new Paragraph(PluginTexts.ConfirmationIntro),
                            ..Presets.CreateAuthElements(req)
                                .Save(out var auth).Elements,
                            new Heading3(CommonTexts.TwoFactorCode),
                            new TextBox("code", CommonTexts.TwoFactorPlaceholder, null, TextBoxRole.NoSpellcheck)
                                .Save(out var codeInput),
                            new ContinueButton()
                        ],
                        async actionReq =>
                        {
                            if (!actionReq.LoggedIn)
                                throw new ForcedActionReload();
                            
                            if (codeInput.IsEmpty(out var code) || auth.AnyEmpty)
                                throw new ForcedActionError(CommonTexts.EmptyFields);
                            
                            if (actionReq.User.TwoFactor.TOTP == null || actionReq.User.TwoFactor.TOTP.Verified)
                                throw new ForcedActionReload();
                            
                            if (!await Presets.ValidateAuth(actionReq, auth))
                                throw new ForcedActionError(auth.CodeInput != null ? PluginTexts.WrongAuthWith2FA : PluginTexts.WrongAuthWithout2FA);
                            
                            if (!await actionReq.UserTable.ValidateTOTPAsync(actionReq.User.Id, code, actionReq, false))
                                throw new ForcedActionError(PluginTexts.CodeInvalid);
                            
                            await actionReq.UserTable.VerifyTOTPAsync(actionReq.User.Id);
                            await Presets.WarningMailAsync(actionReq, actionReq.User, PluginTexts.TwoFactorEnabled, PluginTexts.TwoFactorEnabledMail);
                            page.Navigate("../settings");
                        }
                    )
                ]
            ));
            return page;
        }
    }
    
    [Endpoint("/settings/2fa-codes")]
    protected static IResponse HandleTwoFactorSettingsCodes(Request req)
    {
        req.ForceGET(); req.ForceLogin(false);
        if (req.User.TwoFactor.TOTP == null || req.User.TwoFactor.TOTPEnabled(out _))
            return StatusResponse.NotFound;
        else
            return new ByteArrayDownloadResponse(Encoding.UTF8.GetBytes(string.Join('\n', req.User.TwoFactor.TOTP.RecoveryCodes)), $"2FA {PluginTexts.RecoveryCodes} ({req.Domain}).txt", null);
    }
}