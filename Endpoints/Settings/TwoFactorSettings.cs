using System.Text;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static async Task<Page> HandleTwoFactorSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true, "2FA settings");
        page.Sidebar.Items.ReplaceAll(SettingsSidebar);
        if (req.User.TwoFactor.TOTPEnabled())
        { //2fa enabled and verified, show option to disable it
            page.Sections.Add(new(
                "2FA settings",
                [
                    new ServerForm(
                        null,
                        [
                            new Paragraph("Two-factor authentication is enabled. Enter your password and current 2FA code below to disable it. If you've lost access to your 2FA app, you can also enter one of your recovery codes."),
                            new Paragraph("Warning: Other devices will remain logged in."),
                            ..Presets.CreateAuthElements(req)
                                .Save(out var auth).Elements,
                            new SubmitButton(new("bi bi-arrow-return-right", "Disable"))
                        ],
                        async actionReq =>
                        {
                            req.ForceLogin(false);
                            if (auth.AnyEmpty)
                                return DialogBuilder.DynamicErrorAction(page, "Please authenticate yourself.");

                            if (req.User.TwoFactor.TOTP == null || !req.User.TwoFactor.TOTP.Verified)
                                return new Reload();
                            
                            if (!await Presets.ValidateAuth(actionReq, auth))
                                return DialogBuilder.DynamicErrorAction(page, $"The provided password{(auth.CodeInput != null ? " or 2FA code" : "")} is invalid.");
                            
                            await req.UserTable.DisableTOTPAsync(actionReq.User.Id);
                            await Presets.WarningMailAsync(actionReq, actionReq.User, "2FA disabled", "Two-factor authentication has just been disabled.");
                            return new Navigate("../settings");
                        }
                    )
                ]
            ));
        }
        else
        { //2fa not fully enabled, show option to enable it
            var totp = await req.UserTable.GenerateTOTPAsync(req.User.Id);
            page.Sections.Add(new(
                "2FA settings",
                [
                    new Subsection(
                        null,
                        [
                            new Paragraph("Two-factor authentication is disabled. Follow the steps below to enable it."),
                            new Paragraph("Warning: Other devices will remain logged in.")
                        ]
                    ),
                    new Subsection(
                        "Private key",
                        [
                            new Paragraph("First, scan the QR code using your authenticator app or manually enter the private key below it."),
                            new Image(req, totp.QRImageBase64Src(req.Domain, req.User.Username)) { Style = "max-height: 15rem" },
                            new Paragraph("Key: " + totp.SecretKeyString)
                        ]
                    ),
                    new Subsection(
                        "Recovery codes",
                        [
                            new Paragraph("Next, copy these recovery codes or download them as a file. They can be used like single-use 2FA codes in case you lose access to your authenticator app, so keep them safe."),
                            new BulletList(totp.RecoveryCodes.Select(c => new ListItem(c))),
                            new LinkButton(new("bi bi-download", "Download"), "2fa-codes") { NewTab = true }
                        ]
                    ),
                    new ServerForm(
                        "Confirmation",
                        [
                            new Paragraph("Finally, enter your password and the current code shown by your 2FA app."),
                            ..Presets.CreateAuthElements(req)
                                .Save(out var auth).Elements,
                            new Heading3("2FA code"),
                            new TextBox("code", "Enter the current code...", null, TextBoxRole.NoSpellcheck)
                                .Save(out var codeInput),
                            new SubmitButton(new("bi bi-arrow-return-right", "Enable"))
                        ],
                        async actionReq =>
                        {
                            actionReq.ForceLogin(false);
                            if (codeInput.IsEmpty(out var code) || auth.AnyEmpty)
                                return DialogBuilder.DynamicErrorAction(page, "Please authenticate yourself and enter the current code.");

                            if (actionReq.User.TwoFactor.TOTP == null || actionReq.User.TwoFactor.TOTP.Verified)
                                return new Reload();
                            
                            if (!await Presets.ValidateAuth(actionReq, auth))
                                return DialogBuilder.DynamicErrorAction(page, $"The provided password{(auth.CodeInput != null ? " or 2FA code" : "")} is invalid.");
                            
                            if (!await actionReq.UserTable.ValidateTOTPAsync(actionReq.User.Id, code, actionReq, false))
                                return DialogBuilder.DynamicErrorAction(page, "The provided code is invalid.");
                
                            await actionReq.UserTable.VerifyTOTPAsync(actionReq.User.Id);
                            await Presets.WarningMailAsync(actionReq, actionReq.User, "2FA enabled", "Two-factor authentication has just been enabled.");
                            return new Navigate("../settings");
                        }
                    )
                ]
            ));
        }
        return page;
    }
    
    private static IResponse HandleTwoFactorSettingsCodes(Request req)
    {
        req.ForceGET(); req.ForceLogin(false);
        if (req.User.TwoFactor.TOTP == null || req.User.TwoFactor.TOTPEnabled(out _))
            return StatusResponse.NotFound;
        else
            return new ByteArrayDownloadResponse(Encoding.UTF8.GetBytes(string.Join('\n', req.User.TwoFactor.TOTP.RecoveryCodes)), $"2FA Recovery Codes ({req.Domain}).txt", null);
    }
}