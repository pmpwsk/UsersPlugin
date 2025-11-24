using System.Text;
using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private async Task HandleSettings(Request req)
    {
        switch (req.Path)
        {
            // MAIN SETTINGS PAGE
            case "/settings":
            { req.ForceGET(); req.ForceLogin();
                req.CreatePage("Settings", out var page, out var e);
                page.Navigation.Add(new Button("Back", ".", "right"));
                e.Add(new HeadingElement("Settings", ""));
                e.Add(new ButtonElement("Theme", null, "settings/theme"));
                e.Add(new ButtonElement("Username", null, "settings/username"));
                e.Add(new ButtonElement("Email address", null, "settings/email"));
                e.Add(new ButtonElement("Password", null, "settings/password"));
                e.Add(new ButtonElement("Two-factor authentication", null, "settings/2fa"));
                e.Add(new ButtonElement("Applications", null, "settings/apps"));
                e.Add(new ButtonElement("Delete account", null, "settings/delete"));
            } break;




            // 2FA SETTINGS
            case "/settings/2fa":
            { req.ForceGET(); req.ForceLogin();
                req.CreatePage("2FA settings", out var page, out var e);
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
            } break;

            case "/settings/2fa/recovery":
            { req.ForceGET(); req.ForceLogin(false);
                if (req.User.TwoFactor.TOTP == null || req.User.TwoFactor.TOTPEnabled(out _))
                    req.Status = 404;
                else await req.WriteBytesAsDownload(Encoding.UTF8.GetBytes(string.Join('\n', req.User.TwoFactor.TOTP.RecoveryCodes)), $"2FA Recovery Codes ({req.Domain}).txt");
            } break;
            
            case "/settings/2fa/change":
            { req.ForcePOST(); req.ForceLogin(false);
                if (req.Query.TryGetValue("method", out var method) && req.Query.TryGetValue("code", out var code) && req.Query.TryGetValue("password", out var password))
                    if (req.User.TwoFactor.TOTP == null)
                        await req.Write("2FA not enabled.");
                    else if (!await req.UserTable.ValidatePasswordAsync(req.User.Id, password, req))
                        await req.Write("no");
                    else if (!await req.UserTable.ValidateTOTPAsync(req.User.Id, code, req, method != "enable"))
                        await req.Write("no");
                    else switch (method)
                    {
                        case "enable":
                            if (req.User.TwoFactor.TOTP.Verified)
                                await req.Write("2FA already enabled.");
                            else
                            {
                                await req.UserTable.VerifyTOTPAsync(req.User.Id);
                                await Presets.WarningMailAsync(req, req.User, "2FA enabled", "Two-factor authentication has just been enabled.");
                                await req.Write("ok");
                            }
                            break;
                        case "disable":
                            await req.UserTable.DisableTOTPAsync(req.User.Id);
                            await Presets.WarningMailAsync(req, req.User, "2FA disabled", "Two-factor authentication has just been disabled.");
                            await req.Write("ok");
                            break;
                        default:
                            req.Status = 400;
                            break;
                    }
                else req.Status = 400;
            } break;




            // MANAGE APPS
            case "/settings/apps":
            { req.ForceGET(); req.ForceLogin();
                req.CreatePage("Applications", out var page, out var e);
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
            } break;

            case "/settings/apps/remove":
            { req.ForcePOST(); req.ForceLogin(false);
                if (req.Query.TryGetValue("index", out int index) && index >= 0 && req.Query.TryGetValue("name", out var name) && req.Query.TryGetValue("expires", out long ticks))
                {
                    var kv = req.User.Auth.ElementAtOrDefault(index);
                    if (kv.Equals(default(KeyValuePair<string, AuthTokenData>)))
                        req.Status = 404;
                    else if (kv.Value.FriendlyName == name && kv.Value.Expires.Ticks == ticks)
                        await req.UserTable.DeleteTokenAsync(req.User.Id, kv.Key);
                    else req.Status = 404;
                }
                else req.Status = 400;
            } break;




            // DELETE ACCOUNT
            case "/settings/delete":
            { req.ForceGET(); req.ForceLogin();
                req.CreatePage("Delete account", out var page, out var e);
                page.Navigation.Add(new Button("Back", "../settings", "right"));
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("delete.js"));
                e.Add(new HeadingElement("Delete account",
                [
                    new Paragraph("We're very sad to see you go! If you're leaving because you've been experiencing issues, please let us know and we'll try our best to fix it. The goal of this project is to make your experience as nice as possible."),
                    new Paragraph($"If you really want to delete your account, enter your password{(req.User.TwoFactor.TOTPEnabled()?" and 2FA code":"")} below.")
                ]));
                req.AddAuthElements();
                e.Add(new ButtonElementJS("Delete account :(", null, "Continue()", id: "continueButton"));
                page.AddError();
            } break;

            case "/settings/delete/try":
            { req.ForcePOST(); req.ForceLogin(false);
                if (!await req.Auth(req.User))
                    break;
                req.Cookies.Delete("AuthToken");
                await req.UserTable.DeleteAllTokensAsync(req.User.Id);
                await req.UserTable.SetSettingAsync(req.User.Id, "Delete", DateTime.UtcNow.Ticks.ToString());
                await Presets.WarningMailAsync(req, req.User, "Account deletion", "You just requested your account to be deleted. We will keep your data for another 30 days, in case you change your mind. If you want to restore your account, simply log in again within the next 30 days. If you want us to delete your data immediately, please contact us by replying to this email.");
                await req.Write("ok");
            } break;




            // EMAIL SETTINGS
            case "/settings/email":
            { req.ForceGET(); req.ForceLogin();
                req.CreatePage("Email settings", out var page, out var e);
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
                    req.AddAuthElements();
                    e.Add(new ButtonElementJS("Continue", null, "Continue()", id: "continueButton"));
                    page.AddError();
                }
            } break;

            case "/settings/email/cancel":
            { req.ForcePOST(); req.ForceLogin(false);
                await req.UserTable.DeleteSettingAsync(req.User.Id, "EmailChange");
            } break;

            case "/settings/email/resend":
            { req.ForcePOST(); req.ForceLogin(false);
                if (!req.User.Settings.TryGetValue("EmailChange", out var settingRaw))
                    throw new NotFoundSignal();
                string[] setting = settingRaw.Split('&');
                string mail = HttpUtility.UrlDecode(setting[0]);
                string existingCode = setting[1];
                await Presets.WarningMailAsync(req, req.User, "Email change", $"You requested to change your email address to this address. Your verification code is: {existingCode}", mail);
            } break;

            case "/settings/email/verify":
            { req.ForcePOST(); req.ForceLogin(false);
                if (!req.User.Settings.TryGetValue("EmailChange", out var settingRaw))
                    throw new NotFoundSignal();
                string[] setting = settingRaw.Split('&');
                string mail = HttpUtility.UrlDecode(setting[0]);
                string existingCode = setting[1];
                if (!req.Query.TryGetValue("code", out var code))
                    throw new BadRequestSignal();
                if (code != existingCode)
                {
                    AccountManager.ReportFailedAuth(req.Context);
                    await req.Write("no");
                    break;
                }
                try
                {
                    string oldMail = req.User.MailAddress;
                    await req.UserTable.SetMailAddressAsync(req.User.Id, mail);
                    await Presets.WarningMailAsync(req, req.User, "Email changed", $"Your email was just changed to {mail}.", oldMail);
                    await req.UserTable.DeleteSettingAsync(req.User.Id, "EmailChange");
                    await req.Write("ok");
                }
                catch (Exception ex)
                {
                    await req.Write(ex.Message switch
                    {
                        "Another user with the provided mail address already exists." => "exists",
                        "The provided mail address is the same as the old one." => "same",
                        "Invalid mail address format." => "bad",
                        _ => "error"
                    });
                }
            } break;
            
            case "/settings/email/set":
            { req.ForcePOST(); req.ForceLogin(false);
                if (!await req.Auth(req.User))
                    break;
                if (!req.Query.TryGetValue("email", out var email))
                    req.Status = 400;
                else if (req.User.MailAddress == email)
                    await req.Write("same");
                else if (!AccountManager.CheckMailAddressFormat(email))
                    await req.Write("bad");
                else if (await req.UserTable.FindByMailAddressAsync(email) != null)
                    await req.Write("exists");
                else
                {
                    string code = Parsers.RandomString(10);
                    await req.UserTable.SetSettingAsync(req.User.Id, "EmailChange", $"{HttpUtility.UrlEncode(email)}&{code}");
                    await Presets.WarningMailAsync(req, req.User, "Email change", $"You requested to change your email address to this address. Your verification code is: {code}", email);
                    await req.Write("ok");
                }
            } break;




            // PASSWORD SETTINGS
            case "/settings/password":
            { req.ForceGET(); req.ForceLogin();
                req.CreatePage("Password settings", out var page, out var e);
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
                req.AddAuthElements();
                e.Add(new ButtonElementJS("Change", null, "Continue()", id: "continueButton"));
                page.AddError();
            } break;
            
            case "/settings/password/set":
            { req.ForcePOST(); req.ForceLogin(false);
                if (!req.Query.TryGetValue("new-password", out var password))
                    throw new BadRequestSignal();
                if (!await req.Auth(req.User))
                    break;
                try
                {
                    await req.UserTable.SetPasswordAsync(req.User.Id, password);
                    await Presets.WarningMailAsync(req, req.User, "Password changed", "Your password was just changed.");
                    await req.Write("ok");
                }
                catch (Exception ex)
                {
                    await req.Write(ex.Message switch
                    {
                        "Invalid password format." => "bad",
                        "The provided password is the same as the old one." => "same",
                        _ => "error"
                    });
                }
            } break;

            case "/settings/password/cancel-reset":
            { req.ForcePOST(); req.ForceLogin(false);
                await req.UserTable.DeleteSettingAsync(req.User.Id, "PasswordReset");
            } break;




            // THEME SETTINGS
            case "/settings/theme":
            { req.ForceGET(); req.ForceLogin();
                req.CreatePage("Theme settings", out var page, out var e);
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
            } break;
            
            case "/settings/theme/set":
            { req.ForcePOST(); req.ForceLogin(false);
                if (!ThemeFromQuery(req.Context.Request.QueryString.Value ?? "", out string font, out _, out string background, out string accent, out string design))
                    req.Status = 400;
                await req.UserTable.SetSettingAsync(req.User.Id, "Theme", $"?f={font}&b={background}&a={accent}&d={design}");
            }
            break;




            // USERNAME SETTINGS
            case "/settings/username":
            { req.ForceGET(); req.ForceLogin();
                req.CreatePage("Username settings", out var page, out var e);
                page.Navigation.Add(new Button("Back", "../settings", "right"));
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("username.js"));
                e.Add(new HeadingElement("Username settings", ["Warning: Other devices will remain logged in.", "Current: " + req.User.Username]));
                e.Add(new ContainerElement("New username:", new TextBox("Enter a username...", req.User.Username, "username", TextBoxRole.Username, "Continue()")));
                req.AddAuthElements();
                e.Add(new ButtonElementJS("Change", null, "Continue()", id: "continueButton"));
                page.AddError();
            } break;
                
            case "/settings/username/set":
            { req.ForcePOST(); req.ForceLogin(false);
                if (!req.Query.TryGetValue("username", out var username))
                    throw new BadRequestSignal();
                if (!await req.Auth(req.User))
                    break;
                try
                {
                    await req.UserTable.SetUsernameAsync(req.User.Id, username);
                    await Presets.WarningMailAsync(req, req.User, "Username changed", $"Your username was just changed to {username}.");
                    await req.Write("ok");
                }
                catch (Exception ex)
                {
                    await req.Write(ex.Message switch
                    {
                        "Invalid username format." => "bad",
                        "Another user with the provided username already exists." => "exists",
                        "The provided username is the same as the old one." => "same",
                        _ => "error"
                    });
                }
            } break;




            // 404
            default:
                req.CreatePage("Error");
                req.Status = 404;
                break;
        }
    }
}