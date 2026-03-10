using System.Text;
using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private async Task<IResponse> HandleSettings(Request req)
    {
        switch (req.Path)
        {
            // MAIN SETTINGS PAGE
            case "/settings":
            { req.ForceGET(); req.ForceLogin();
                var page = new Page(req, false);
                page.Title = "Settings";
                page.Sections.Add(new(
                    "Settings",
                    [
                        new Subsection(
                            null,
                            [
                                new BigLinkButton(new("bi bi-eye", "Theme"), [ "Adjust the UI theme." ], "settings/theme"),
                                new BigLinkButton(new("bi bi-person", "Username"), [ "Change your username." ], "settings/username"),
                                new BigLinkButton(new("bi bi-envelope", "Email address"), [ "Change your email address." ], "settings/email"),
                                new BigLinkButton(new("bi bi-key", "Password"), [ "Change your password." ], "settings/password"),
                                new BigLinkButton(new("bi bi-lock", "Two-factor authentication"), [ $"{(req.User.TwoFactor.TOTPEnabled() ? "Disable" : "Enable")} 2FA." ], "settings/2fa"),
                                new BigLinkButton(new("bi bi-hdd-rack", "Applications"), [ "Manage apps that can access your account." ], "settings/apps"),
                                new BigLinkButton(new("bi bi-trash", "Delete account"), [ "Close your account." ], "settings/delete")
                            ]
                        )
                ]));
                return page;
            }




            // 2FA SETTINGS
            case "/settings/2fa":
            { req.ForceGET(); req.ForceLogin();
                Presets.CreatePage(req, "2FA settings", out var page, out var e);
                page.Navigation.Add(new Elements.Button("Back", "../settings", "right"));
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Elements.Script($"2fa.js"));
                if (req.User.TwoFactor.TOTPEnabled())
                { //2fa enabled and verified, show option to disable it
                    e.Add(new Elements.HeadingElement("2FA settings", ["Two-factor authentication is enabled. Enter your password and current 2FA code below to disable it. If you lost access to your 2FA app, you can also enter one of your recovery codes.", "Warning: Other devices will remain logged in."]));
                    e.Add(new Elements.ContainerElement(null,
                    [
                        new Elements.Heading("Password:"),
                        new Elements.TextBox("Enter your password...", null, "password", Elements.TextBoxRole.Password, "Continue('disable')"),
                        new Elements.Heading("2FA code / recovery:"),
                        new Elements.TextBox("Enter the current code...", null, "code", Elements.TextBoxRole.NoSpellcheck, "Continue('disable')")
                    ]));
                    e.Add(new Elements.ButtonElementJS("Disable", null, "Continue('disable')", id: "continueButton"));
                    page.AddError();
                }
                else
                { //2fa not fully enabled, show option to enable it
                    var totp = await req.UserTable.GenerateTOTPAsync(req.User.Id);
                    e.Add(new Elements.HeadingElement("2FA settings", ["Two-factor authentication is disabled. Follow the steps below to enable it.", "Warning: Other devices will remain logged in."]));
                    e.Add(new Elements.ContainerElement("Private key:",
                    [
                        new Elements.Paragraph("First, scan the QR code using your authenticator app or manually enter the private key below it."),
                        new Elements.Image(totp.QRImageBase64Src(req.Domain, req.User.Username), "max-height: 15rem"),
                        new Elements.Paragraph("Key: " + totp.SecretKeyString)
                    ]));
                    e.Add(new Elements.ContainerElement("Recovery codes:", new Elements.Paragraph("Next, copy these recovery codes or download them as a file. They can be used like single-use 2FA codes in case you lose access to your authenticator app, so keep them safe.<br /><br />" + string.Join("<br />", totp.RecoveryCodes)) {Unsafe = true})
                    { Button = new Elements.Button("Download", $"2fa/recovery", newTab: true) });
                    e.Add(new Elements.ContainerElement("Confirm:",
                    [
                        new Elements.Paragraph("Finally, enter your password and the current code shown by your 2FA app."),
                        new Elements.Heading("Password:"),
                        new Elements.TextBox("Enter your password...", null, "password", Elements.TextBoxRole.Password, "Continue('enable')"),
                        new Elements.Heading("2FA code:"),
                        new Elements.TextBox("Enter the current code...", null, "code", Elements.TextBoxRole.NoSpellcheck, "Continue('enable')")
                    ]));
                    e.Add(new Elements.ButtonElementJS("Enable", null, "Continue('enable')", id: "continueButton"));
                    page.AddError();
                }
                return new LegacyPageResponse(page, req);
            }

            case "/settings/2fa/recovery":
            { req.ForceGET(); req.ForceLogin(false);
                if (req.User.TwoFactor.TOTP == null || req.User.TwoFactor.TOTPEnabled(out _))
                    return StatusResponse.NotFound;
                else
                    return new ByteArrayDownloadResponse(Encoding.UTF8.GetBytes(string.Join('\n', req.User.TwoFactor.TOTP.RecoveryCodes)), $"2FA Recovery Codes ({req.Domain}).txt", null);
            }
            
            case "/settings/2fa/change":
            { req.ForcePOST(); req.ForceLogin(false);
                var method = req.Query.GetOrThrow("method");
                var code = req.Query.GetOrThrow("code");
                var password = req.Query.GetOrThrow("password");
                if (req.User.TwoFactor.TOTP == null)
                    return new TextResponse("2FA not enabled.");
                if (!await req.UserTable.ValidatePasswordAsync(req.User.Id, password, req)
                    || !await req.UserTable.ValidateTOTPAsync(req.User.Id, code, req, method != "enable"))
                    return new TextResponse("no");
                switch (method)
                {
                    case "enable":
                        if (req.User.TwoFactor.TOTP.Verified)
                            return new TextResponse("2FA already enabled.");
                        
                        await req.UserTable.VerifyTOTPAsync(req.User.Id);
                        await Presets.WarningMailAsync(req, req.User, "2FA enabled", "Two-factor authentication has just been enabled.");
                        return new TextResponse("ok");
                    case "disable":
                        await req.UserTable.DisableTOTPAsync(req.User.Id);
                        await Presets.WarningMailAsync(req, req.User, "2FA disabled", "Two-factor authentication has just been disabled.");
                        return new TextResponse("ok");
                    default:
                        return StatusResponse.BadRequest;
                }
            }




            // MANAGE APPS
            case "/settings/apps":
            { req.ForceGET(); req.ForceLogin();
                Presets.CreatePage(req, "Applications", out var page, out var e);
                page.Navigation.Add(new Elements.Button("Back", "../settings", "right"));
                page.Scripts.Add(new Elements.Script("apps.js"));
                e.Add(new Elements.HeadingElement("Applications", "These are the applications that have partial access to your account."));
                page.AddError();
                int index = 0;
                bool foundAny = false;
                foreach (var kv in req.User.Auth)
                {
                    if (kv.Value.LimitedToPaths != null)
                    {
                        foundAny = true;
                        e.Add(new Elements.ContainerElement(kv.Value.FriendlyName,
                        [
                            new Elements.Paragraph($"Expires: {kv.Value.Expires} UTC"),
                            new Elements.BulletList(kv.Value.LimitedToPaths)
                        ]) { Button = new Elements.ButtonJS("Remove", $"Remove('{index}','{HttpUtility.UrlEncode(kv.Value.FriendlyName)}','{kv.Value.Expires.Ticks}')", "red") });
                    }
                    index++;
                }
                if (!foundAny)
                    e.Add(new Elements.ContainerElement("No applications!", "", classes: "red"));
                return new LegacyPageResponse(page, req);
            }

            case "/settings/apps/remove":
            { req.ForcePOST(); req.ForceLogin(false);
                if (req.Query.TryGetValue("index", out int index) && index >= 0 && req.Query.TryGetValue("name", out var name) && req.Query.TryGetValue("expires", out long ticks))
                {
                    var kv = req.User.Auth.ElementAtOrDefault(index);
                    if (kv.Equals(default(KeyValuePair<string, AuthTokenData>)))
                        return StatusResponse.NotFound;
                    else if (kv.Value.FriendlyName == name && kv.Value.Expires.Ticks == ticks)
                    {
                        await req.UserTable.DeleteTokenAsync(req.User.Id, kv.Key);
                        return StatusResponse.Success;
                    }
                    else
                        return StatusResponse.NotFound;
                }
                else
                    return StatusResponse.BadRequest;
            }




            // DELETE ACCOUNT
            case "/settings/delete":
            { req.ForceGET(); req.ForceLogin();
                Presets.CreatePage(req, "Delete account", out var page, out var e);
                page.Navigation.Add(new Elements.Button("Back", "../settings", "right"));
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Elements.Script("delete.js"));
                e.Add(new Elements.HeadingElement("Delete account",
                [
                    new Elements.Paragraph("We're very sad to see you go! If you're leaving because you've been experiencing issues, please let us know and we'll try our best to fix it. The goal of this project is to make your experience as nice as possible."),
                    new Elements.Paragraph($"If you really want to delete your account, enter your password{(req.User.TwoFactor.TOTPEnabled()?" and 2FA code":"")} below.")
                ]));
                Presets.AddAuthElements(page, req);
                e.Add(new Elements.ButtonElementJS("Delete account :(", null, "Continue()", id: "continueButton"));
                page.AddError();
                return new LegacyPageResponse(page, req);
            }

            case "/settings/delete/try":
            { req.ForcePOST(); req.ForceLogin(false);
                await req.Auth(req.User);
                req.CookieWriter?.Delete("AuthToken");
                await req.UserTable.DeleteAllTokensAsync(req.User.Id);
                await req.UserTable.SetSettingAsync(req.User.Id, "Delete", DateTime.UtcNow.Ticks.ToString());
                await Presets.WarningMailAsync(req, req.User, "Account deletion", "You just requested your account to be deleted. We will keep your data for another 30 days, in case you change your mind. If you want to restore your account, simply log in again within the next 30 days. If you want us to delete your data immediately, please contact us by replying to this email.");
                return new TextResponse("ok");
            }




            // EMAIL SETTINGS
            case "/settings/email":
            { req.ForceGET(); req.ForceLogin();
                var page = new Page(req, true);
                page.Title = "Email settings";
                if (req.User.Settings.TryGetValue("EmailChange", out var settingRaw))
                {
                    string[] setting = settingRaw.Split('&');
                    string mail = HttpUtility.UrlDecode(setting[0]);
                    var codeInput = new TextBox("code", "Enter the code...", null, TextBoxRole.NoSpellcheck) { Autofocus = true };
                    page.Sections.Add(new(
                        "Email settings",
                        [
                            new Subsection(
                                null,
                                [
                                    new Paragraph($"You requested to change your email to '{mail}'. Please enter the verification code provided in the email we sent to that address here."),
                                    new ServerActionButton("Cancel", async actionReq =>
                                    {
                                        req.ForceLogin(false);
                                        await req.UserTable.DeleteSettingAsync(actionReq.User.Id, "EmailChange");
                                        return new Reload();
                                    }),
                                    new ServerActionButton("Resend", actionReq => TryResendEmailChange(actionReq, page))
                                ]
                            ),
                            new ServerForm(
                                null,
                                actionReq => TryVerifyEmailChange(actionReq, page, codeInput.Value),
                                [
                                    new Heading3("Verification code"),
                                    codeInput,
                                    new SubmitButton("Change")
                                ]
                            )
                        ]
                    ));
                }
                else
                {
                    var emailInput = new TextBox("email", "Enter your email address...", null, TextBoxRole.Email) { Autofocus = true };
                    var auth = Presets.CreateAuthElements(req);
                    page.Sections.Add(new(
                        "Email settings",
                        [
                            new ServerForm(
                                null,
                                actionReq => TryRequestEmailChange(actionReq, page, emailInput.Value, auth),
                                [
                                    new Paragraph($"Current: {req.User.MailAddress}"),
                                    new Heading3("New email"),
                                    emailInput,
                                    ..auth.Elements,
                                    new SubmitButton("Continue")
                                ]
                            )
                        ]
                    ));
                }
                return page;
            }

            case "/settings/email/cancel":
            { req.ForcePOST(); req.ForceLogin(false);
                await req.UserTable.DeleteSettingAsync(req.User.Id, "EmailChange");
                return StatusResponse.Success;
            }

            case "/settings/email/resend":
            { req.ForcePOST(); req.ForceLogin(false);
                if (!req.User.Settings.TryGetValue("EmailChange", out var settingRaw))
                    return StatusResponse.NotFound;
                string[] setting = settingRaw.Split('&');
                string mail = HttpUtility.UrlDecode(setting[0]);
                string existingCode = setting[1];
                await Presets.WarningMailAsync(req, req.User, "Email change", $"You requested to change your email address to this address. Your verification code is: {existingCode}", mail);
                return StatusResponse.Success;
            }

            case "/settings/email/verify":
            { req.ForcePOST(); req.ForceLogin(false);
                if (!req.User.Settings.TryGetValue("EmailChange", out var settingRaw))
                    return StatusResponse.NotFound;
                string[] setting = settingRaw.Split('&');
                string mail = HttpUtility.UrlDecode(setting[0]);
                string existingCode = setting[1];
                var code = req.Query.GetOrThrow("code");
                if (code != existingCode)
                {
                    AccountManager.ReportFailedAuth(req);
                    return new TextResponse("no");
                }
                try
                {
                    string oldMail = req.User.MailAddress;
                    await req.UserTable.SetMailAddressAsync(req.User.Id, mail);
                    await Presets.WarningMailAsync(req, req.User, "Email changed", $"Your email was just changed to {mail}.", oldMail);
                    await req.UserTable.DeleteSettingAsync(req.User.Id, "EmailChange");
                    return new TextResponse("ok");
                }
                catch (Exception ex)
                {
                    return new TextResponse(ex.Message switch
                    {
                        "Another user with the provided mail address already exists." => "exists",
                        "The provided mail address is the same as the old one." => "same",
                        "Invalid mail address format." => "bad",
                        _ => "error"
                    });
                }
            }
            
            case "/settings/email/set":
            { req.ForcePOST(); req.ForceLogin(false);
                await req.Auth(req.User);
                var email = req.Query.GetOrThrow("email");
                if (req.User.MailAddress == email)
                    return new TextResponse("same");
                else if (!AccountManager.CheckMailAddressFormat(email))
                    return new TextResponse("bad");
                else if (await req.UserTable.FindByMailAddressAsync(email) != null)
                    return new TextResponse("exists");
                else
                {
                    string code = Parsers.RandomString(10);
                    await req.UserTable.SetSettingAsync(req.User.Id, "EmailChange", $"{HttpUtility.UrlEncode(email)}&{code}");
                    await Presets.WarningMailAsync(req, req.User, "Email change", $"You requested to change your email address to this address. Your verification code is: {code}", email);
                    return new TextResponse("ok");
                }
            }




            // PASSWORD SETTINGS
            case "/settings/password":
            { req.ForceGET(); req.ForceLogin();
                Presets.CreatePage(req, "Password settings", out var page, out var e);
                page.Navigation.Add(new Elements.Button("Back", "../settings", "right"));
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Elements.Script("password.js"));
                e.Add(new Elements.HeadingElement("Password settings", "Warning: Other devices will remain logged in."));
                if (req.User.Settings.ContainsKey("PasswordReset"))
                    e.Add(new Elements.ContainerElement("Warning", "A password reset has been requested and a corresponding link has been sent to your email address.", "red")
                    { Button = new Elements.ButtonJS("Cancel", "Cancel()", "red") });
                e.Add(new Elements.ContainerElement(null,
                [
                    new Elements.Heading("New password:"),
                    new Elements.TextBox("Enter a password...", null, "password1", Elements.TextBoxRole.NewPassword, "Continue()"),
                    new Elements.Heading("Confirm password:"),
                    new Elements.TextBox("Enter the password again...", null, "password2", Elements.TextBoxRole.NewPassword, "Continue()")
                ]));
                Presets.AddAuthElements(page, req);
                e.Add(new Elements.ButtonElementJS("Change", null, "Continue()", id: "continueButton"));
                page.AddError();
                return new LegacyPageResponse(page, req);
            }
            
            case "/settings/password/set":
            { req.ForcePOST(); req.ForceLogin(false);
                var password = req.Query.GetOrThrow("new-password");
                await req.Auth(req.User);
                try
                {
                    await req.UserTable.SetPasswordAsync(req.User.Id, password);
                    await Presets.WarningMailAsync(req, req.User, "Password changed", "Your password was just changed.");
                    return new TextResponse("ok");
                }
                catch (Exception ex)
                {
                    return new TextResponse(ex.Message switch
                    {
                        "Invalid password format." => "bad",
                        "The provided password is the same as the old one." => "same",
                        _ => "error"
                    });
                }
            }

            case "/settings/password/cancel-reset":
            { req.ForcePOST(); req.ForceLogin(false);
                await req.UserTable.DeleteSettingAsync(req.User.Id, "PasswordReset");
                return StatusResponse.Success;
            }




            // THEME SETTINGS
            case "/settings/theme":
            { req.ForceGET(); req.ForceLogin();
                Presets.CreatePage(req, "Theme settings", out var page, out var e);
                page.Navigation.Add(new Elements.Button("Back", "../settings", "right"));
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Elements.Script("theme.js"));
                e.Add(new Elements.HeadingElement("Theme settings"));
                page.AddError();
                ThemeFromQuery((req.LoggedIn && req.User.Settings.TryGetValue("Theme", out string? theme)) ? theme : "default", out string font, out string? _, out string background, out string accent, out string design);
                e.Add(new Elements.ContainerElement("Background", new Elements.Selector("background", background, [..Backgrounds]) { OnChange = "Save()" }));
                e.Add(new Elements.ContainerElement("Accent", new Elements.Selector("accent", accent, [..Accents]) { OnChange = "Save()" }));
                e.Add(new Elements.ContainerElement("Design", new Elements.Selector("design", design, [..Designs]) { OnChange = "Save()" }));
                e.Add(new Elements.ContainerElement("Font", new Elements.Selector("font", font, [..Fonts]) { OnChange = "Save()" }));
                return new LegacyPageResponse(page, req);
            }
            
            case "/settings/theme/set":
            { req.ForcePOST(); req.ForceLogin(false);
                if (!ThemeFromQuery(req.Query.FullString, out string font, out _, out string background, out string accent, out string design))
                    return StatusResponse.BadRequest;
                await req.UserTable.SetSettingAsync(req.User.Id, "Theme", $"?f={font}&b={background}&a={accent}&d={design}");
                return StatusResponse.Success;
            }




            // USERNAME SETTINGS
            case "/settings/username":
            { req.ForceGET(); req.ForceLogin();
                var page = new Page(req, true);
                page.Title = "Username";
                var usernameBox = new TextBox("username", "Enter a username...", null, TextBoxRole.Username) { Autofocus = true };
                var auth = Presets.CreateAuthElements(req);
                page.Sections.Add(new(
                    "Username settings",
                    [
                        new ServerForm(
                            null,
                            actionReq => TryChangeUsername(actionReq, page, usernameBox.Value, auth),
                            [
                                new Paragraph("Warning: Other devices will remain logged in."),
                                new Paragraph("Current: " + req.User.Username),
                                new Heading3("New username"),
                                usernameBox,
                                ..auth.Elements,
                                new SubmitButton("Change")
                            ]
                        )
                    ]
                ));
                return page;
            }




            // 404
            default:
                return StatusResponse.NotFound;
        }
    }
    
    private static async Task<IActionResponse> TryChangeUsername(Request actionReq, Page page, string username, Presets.AuthElements auth)
    {
        actionReq.ForceLogin(false);
        if (string.IsNullOrEmpty(username) || auth.AnyEmpty)
            return page.DynamicErrorAction("Please enter a username and authenticate yourself.");
            
        if (!await Presets.ValidateAuth(actionReq, auth))
            return page.DynamicErrorAction("Invalid password or 2FA code.");
        
        try
        {
            await actionReq.UserTable.SetUsernameAsync(actionReq.User.Id, username);
            await Presets.WarningMailAsync(actionReq, actionReq.User, "Username changed", $"Your username was just changed to {username}.");
            return new Navigate("../settings");
        }
        catch (Exception ex)
        {
            return page.DynamicErrorAction(ex.Message switch
            {
                "Invalid username format." => "Usernames must be at least 3 characters long and only contain lowercase letters, digits, dashes, dots and underscores. The first and last characters can only be letters or digits.",
                "Another user with the provided username already exists." => "This username is already being used by another account.",
                "The provided username is the same as the old one." => "The provided username is the same as the old one.",
                _ => "error"
            });
        }
    }
    
    private static async Task<IActionResponse> TryRequestEmailChange(Request actionReq, Page page, string email, Presets.AuthElements auth)
    {
        actionReq.ForceLogin(false);
        if (string.IsNullOrEmpty(email) || auth.AnyEmpty)
            return page.DynamicErrorAction("Please enter an email address and authenticate yourself.");
        
        if (!await Presets.ValidateAuth(actionReq, auth))
            return page.DynamicErrorAction("Invalid password or 2FA code.");
        
        if (actionReq.User.MailAddress == email)
            return page.DynamicErrorAction("The provided email address is the same as the old one.");
        
        if (!AccountManager.CheckMailAddressFormat(email))
            return page.DynamicErrorAction("Invalid email address.");
        if (await actionReq.UserTable.FindByMailAddressAsync(email) != null)
            return page.DynamicErrorAction("This email address is already being used by another account.");
        
        string code = Parsers.RandomString(10);
        await actionReq.UserTable.SetSettingAsync(actionReq.User.Id, "EmailChange", $"{HttpUtility.UrlEncode(email)}&{code}");
        await Presets.WarningMailAsync(actionReq, actionReq.User, "Email change", $"You requested to change your email address to this address. Your verification code is: {code}", email);
        return new Navigate("email");
    }
    
    private static async Task<IActionResponse> TryVerifyEmailChange(Request actionReq, Page page, string code)
    {
        actionReq.ForceLogin(false);
        if (string.IsNullOrEmpty(code))
            return page.DynamicErrorAction("Please enter the verification code.");
        
        if (!actionReq.User.Settings.TryGetValue("EmailChange", out var settingRaw))
            return new Reload();
        string[] setting = settingRaw.Split('&');
        string mail = HttpUtility.UrlDecode(setting[0]);
        string existingCode = setting[1];
        if (code != existingCode)
        {
            AccountManager.ReportFailedAuth(actionReq);
            return page.DynamicErrorAction("Invalid code.");
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
            return page.DynamicErrorAction(ex.Message switch
            {
                "Another user with the provided mail address already exists." => "This email address is already being used by another account.",
                "The provided mail address is the same as the old one." => "The provided email address is the same as the old one.",
                "Invalid mail address format." => "Invalid email address.",
                _ => "error"
            });
        }
    }
    
    private static async Task<IActionResponse> TryResendEmailChange(Request actionReq, Page page)
    {
        if (!actionReq.User.Settings.TryGetValue("EmailChange", out var settingRaw))
            return new Reload();
        string[] setting = settingRaw.Split('&');
        string mail = HttpUtility.UrlDecode(setting[0]);
        string existingCode = setting[1];
        await Presets.WarningMailAsync(actionReq, actionReq.User, "Email change", $"You requested to change your email address to this address. Your verification code is: {existingCode}", mail);
        return page.DynamicInfoAction("The code has been sent.");
    }
}