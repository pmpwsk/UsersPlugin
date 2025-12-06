using System.Text;
using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;
using uwap.WebFramework.Responses;

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
                Presets.CreatePage(req, "Settings", out var page, out var e);
                page.Navigation.Add(new Button("Back", ".", "right"));
                e.Add(new HeadingElement("Settings", ""));
                e.Add(new ButtonElement("Theme", null, "settings/theme"));
                e.Add(new ButtonElement("Username", null, "settings/username"));
                e.Add(new ButtonElement("Email address", null, "settings/email"));
                e.Add(new ButtonElement("Password", null, "settings/password"));
                e.Add(new ButtonElement("Two-factor authentication", null, "settings/2fa"));
                e.Add(new ButtonElement("Applications", null, "settings/apps"));
                e.Add(new ButtonElement("Delete account", null, "settings/delete"));
                return new LegacyPageResponse(page, req);
            }




            // 2FA SETTINGS
            case "/settings/2fa":
            { req.ForceGET(); req.ForceLogin();
                Presets.CreatePage(req, "2FA settings", out var page, out var e);
                page.Navigation.Add(new Button("Back", "../settings", "right"));
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script($"2fa.js"));
                if (req.User.TwoFactor.TOTPEnabled())
                { //2fa enabled and verified, show option to disable it
                    e.Add(new HeadingElement("2FA settings", ["Two-factor authentication is enabled. Enter your password and current 2FA code below to disable it. If you lost access to your 2FA app, you can also enter one of your recovery codes.", "Warning: Other devices will remain logged in."]));
                    e.Add(new ContainerElement(null,
                    [
                        new Heading("Password:"),
                        new TextBox("Enter your password...", null, "password", TextBoxRole.Password, "Continue('disable')"),
                        new Heading("2FA code / recovery:"),
                        new TextBox("Enter the current code...", null, "code", TextBoxRole.NoSpellcheck, "Continue('disable')")
                    ]));
                    e.Add(new ButtonElementJS("Disable", null, "Continue('disable')", id: "continueButton"));
                    page.AddError();
                }
                else
                { //2fa not fully enabled, show option to enable it
                    var totp = await req.UserTable.GenerateTOTPAsync(req.User.Id);
                    e.Add(new HeadingElement("2FA settings", ["Two-factor authentication is disabled. Follow the steps below to enable it.", "Warning: Other devices will remain logged in."]));
                    e.Add(new ContainerElement("Private key:",
                    [
                        new Paragraph("First, scan the QR code using your authenticator app or manually enter the private key below it."),
                        new Image(totp.QRImageBase64Src(req.Domain, req.User.Username), "max-height: 15rem"),
                        new Paragraph("Key: " + totp.SecretKeyString)
                    ]));
                    e.Add(new ContainerElement("Recovery codes:", new Paragraph("Next, copy these recovery codes or download them as a file. They can be used like single-use 2FA codes in case you lose access to your authenticator app, so keep them safe.<br /><br />" + string.Join("<br />", totp.RecoveryCodes)) {Unsafe = true})
                    { Button = new Button("Download", $"2fa/recovery", newTab: true) });
                    e.Add(new ContainerElement("Confirm:",
                    [
                        new Paragraph("Finally, enter your password and the current code shown by your 2FA app."),
                        new Heading("Password:"),
                        new TextBox("Enter your password...", null, "password", TextBoxRole.Password, "Continue('enable')"),
                        new Heading("2FA code:"),
                        new TextBox("Enter the current code...", null, "code", TextBoxRole.NoSpellcheck, "Continue('enable')")
                    ]));
                    e.Add(new ButtonElementJS("Enable", null, "Continue('enable')", id: "continueButton"));
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
                page.Navigation.Add(new Button("Back", "../settings", "right"));
                page.Scripts.Add(new Script("apps.js"));
                e.Add(new HeadingElement("Applications", "These are the applications that have partial access to your account."));
                page.AddError();
                int index = 0;
                bool foundAny = false;
                foreach (var kv in req.User.Auth)
                {
                    if (kv.Value.LimitedToPaths != null)
                    {
                        foundAny = true;
                        e.Add(new ContainerElement(kv.Value.FriendlyName,
                        [
                            new Paragraph($"Expires: {kv.Value.Expires} UTC"),
                            new BulletList(kv.Value.LimitedToPaths)
                        ]) { Button = new ButtonJS("Remove", $"Remove('{index}','{HttpUtility.UrlEncode(kv.Value.FriendlyName)}','{kv.Value.Expires.Ticks}')", "red") });
                    }
                    index++;
                }
                if (!foundAny)
                    e.Add(new ContainerElement("No applications!", "", classes: "red"));
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
                page.Navigation.Add(new Button("Back", "../settings", "right"));
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("delete.js"));
                e.Add(new HeadingElement("Delete account",
                [
                    new Paragraph("We're very sad to see you go! If you're leaving because you've been experiencing issues, please let us know and we'll try our best to fix it. The goal of this project is to make your experience as nice as possible."),
                    new Paragraph($"If you really want to delete your account, enter your password{(req.User.TwoFactor.TOTPEnabled()?" and 2FA code":"")} below.")
                ]));
                Presets.AddAuthElements(page, req);
                e.Add(new ButtonElementJS("Delete account :(", null, "Continue()", id: "continueButton"));
                page.AddError();
                return new LegacyPageResponse(page, req);
            }

            case "/settings/delete/try":
            { req.ForcePOST(); req.ForceLogin(false);
                await req.Auth(req.User);
                req.Cookies.Delete("AuthToken");
                await req.UserTable.DeleteAllTokensAsync(req.User.Id);
                await req.UserTable.SetSettingAsync(req.User.Id, "Delete", DateTime.UtcNow.Ticks.ToString());
                await Presets.WarningMailAsync(req, req.User, "Account deletion", "You just requested your account to be deleted. We will keep your data for another 30 days, in case you change your mind. If you want to restore your account, simply log in again within the next 30 days. If you want us to delete your data immediately, please contact us by replying to this email.");
                return new TextResponse("ok");
            }




            // EMAIL SETTINGS
            case "/settings/email":
            { req.ForceGET(); req.ForceLogin();
                Presets.CreatePage(req, "Email settings", out var page, out var e);
                page.Navigation.Add(new Button("Back", "../settings", "right"));
                if (req.User.Settings.TryGetValue("EmailChange", out var settingRaw))
                {
                    string[] setting = settingRaw.Split('&');
                    string mail = HttpUtility.UrlDecode(setting[0]);
                    page.Scripts.Add(Presets.SendRequestScript);
                    page.Scripts.Add(new Script("email-verify.js"));
                    e.Add(new HeadingElement("Email settings", $"You requested to change your email to '{mail}'. Please enter the verification code provided in the email we sent to that address here.")
                    { Button = new ButtonJS("Cancel", "Cancel()", "red") });
                    e.Add(new ContainerElement("Verification code", new TextBox("Enter the code...", null, "code", TextBoxRole.NoSpellcheck, "Continue()", autofocus: true))
                    { Button = new ButtonJS("Send again", "Resend()", id: "resendButton") });
                    e.Add(new ButtonElementJS("Change", null, "Continue()", id: "continueButton"));
                    page.AddError();
                }
                else
                {
                    page.Scripts.Add(Presets.SendRequestScript);
                    page.Scripts.Add(new Script("email.js"));
                    e.Add(new HeadingElement("Email settings", $"Current: {req.User.MailAddress}"));
                    e.Add(new ContainerElement("New email:", new TextBox("Enter the email address...", req.User.MailAddress, "email", TextBoxRole.Email, "Continue()")));
                    Presets.AddAuthElements(page, req);
                    e.Add(new ButtonElementJS("Continue", null, "Continue()", id: "continueButton"));
                    page.AddError();
                }
                return new LegacyPageResponse(page, req);
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
                page.Navigation.Add(new Button("Back", "../settings", "right"));
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("password.js"));
                e.Add(new HeadingElement("Password settings", "Warning: Other devices will remain logged in."));
                if (req.User.Settings.ContainsKey("PasswordReset"))
                    e.Add(new ContainerElement("Warning", "A password reset has been requested and a corresponding link has been sent to your email address.", "red")
                    { Button = new ButtonJS("Cancel", "Cancel()", "red") });
                e.Add(new ContainerElement(null,
                [
                    new Heading("New password:"),
                    new TextBox("Enter a password...", null, "password1", TextBoxRole.NewPassword, "Continue()"),
                    new Heading("Confirm password:"),
                    new TextBox("Enter the password again...", null, "password2", TextBoxRole.NewPassword, "Continue()")
                ]));
                Presets.AddAuthElements(page, req);
                e.Add(new ButtonElementJS("Change", null, "Continue()", id: "continueButton"));
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
                page.Navigation.Add(new Button("Back", "../settings", "right"));
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("theme.js"));
                e.Add(new HeadingElement("Theme settings"));
                page.AddError();
                ThemeFromQuery((req.LoggedIn && req.User.Settings.TryGetValue("Theme", out string? theme)) ? theme : "default", out string font, out string? _, out string background, out string accent, out string design);
                e.Add(new ContainerElement("Background", new Selector("background", background, [..Backgrounds]) { OnChange = "Save()" }));
                e.Add(new ContainerElement("Accent", new Selector("accent", accent, [..Accents]) { OnChange = "Save()" }));
                e.Add(new ContainerElement("Design", new Selector("design", design, [..Designs]) { OnChange = "Save()" }));
                e.Add(new ContainerElement("Font", new Selector("font", font, [..Fonts]) { OnChange = "Save()" }));
                return new LegacyPageResponse(page, req);
            }
            
            case "/settings/theme/set":
            { req.ForcePOST(); req.ForceLogin(false);
                if (!ThemeFromQuery(req.QueryString, out string font, out _, out string background, out string accent, out string design))
                    return StatusResponse.BadRequest;
                await req.UserTable.SetSettingAsync(req.User.Id, "Theme", $"?f={font}&b={background}&a={accent}&d={design}");
                return StatusResponse.Success;
            }




            // USERNAME SETTINGS
            case "/settings/username":
            { req.ForceGET(); req.ForceLogin();
                Presets.CreatePage(req, "Username settings", out var page, out var e);
                page.Navigation.Add(new Button("Back", "../settings", "right"));
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("username.js"));
                e.Add(new HeadingElement("Username settings", ["Warning: Other devices will remain logged in.", "Current: " + req.User.Username]));
                e.Add(new ContainerElement("New username:", new TextBox("Enter a username...", req.User.Username, "username", TextBoxRole.Username, "Continue()")));
                Presets.AddAuthElements(page, req);
                e.Add(new ButtonElementJS("Change", null, "Continue()", id: "continueButton"));
                page.AddError();
                return new LegacyPageResponse(page, req);
            }
                
            case "/settings/username/set":
            { req.ForcePOST(); req.ForceLogin(false);
                var username = req.Query.GetOrThrow("username");
                await req.Auth(req.User);
                try
                {
                    await req.UserTable.SetUsernameAsync(req.User.Id, username);
                    await Presets.WarningMailAsync(req, req.User, "Username changed", $"Your username was just changed to {username}.");
                    return new TextResponse("ok");
                }
                catch (Exception ex)
                {
                    return new TextResponse(ex.Message switch
                    {
                        "Invalid username format." => "bad",
                        "Another user with the provided username already exists." => "exists",
                        "The provided username is the same as the old one." => "same",
                        _ => "error"
                    });
                }
            }




            // 404
            default:
                return StatusResponse.NotFound;
        }
    }
}