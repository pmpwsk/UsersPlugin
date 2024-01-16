using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public void Settings(AppRequest request, string path, string pathPrefix)
    {
        if (NotLoggedIn(request)) return;
        request.Init(out Page page, out List<IPageElement> e);
        User user = request.User;
        switch (path)
        {
            case "":
                page.Title = "Settings";
                e.Add(new HeadingElement("Settings", ""));
                e.Add(new ButtonElement("Theme", null, $"{pathPrefix}/settings/theme"));
                e.Add(new ButtonElement("Username", null, $"{pathPrefix}/settings/username"));
                e.Add(new ButtonElement("Email address", null, $"{pathPrefix}/settings/email"));
                e.Add(new ButtonElement("Password", null, $"{pathPrefix}/settings/password"));
                e.Add(new ButtonElement("Two-factor authentication", null, $"{pathPrefix}/settings/2fa"));
                e.Add(new ButtonElement("Delete account", null, $"{pathPrefix}/settings/delete"));
                break;
            case "/theme":
                {
                    page.Scripts.Add(new Script($"{pathPrefix}/settings/theme.js"));
                    page.Title = "Theme settings";
                    e.Add(new HeadingElement("Theme settings", ""));
                    ThemeFromQuery((request.LoggedIn && request.User.Settings.TryGetValue("Theme", out string? theme)) ? theme : "default", out string font, out string? fontMono, out string background, out string accent, out string design);
                    e.Add(new ContainerElement("Background", new Selector("background", background, [..Backgrounds]) { OnChange = "Save()" }));
                    e.Add(new ContainerElement("Accent", new Selector("accent", accent, [..Accents]) { OnChange = "Save()" }));
                    e.Add(new ContainerElement("Design", new Selector("design", design, [..Designs]) { OnChange = "Save()" }));
                    e.Add(new ContainerElement("Font", new Selector("font", font, [..Fonts]) { OnChange = "Save()" }));
                }
                break;
            case "/2fa":
                {
                    page.Title = "2FA settings";
                    page.Scripts.Add(new Script($"{pathPrefix}/settings/2fa.js"));
                    if (user.TwoFactor.TOTPEnabled())
                    { //2fa enabled and verified, show option to disable it
                        e.Add(new HeadingElement("2FA settings", "Two-factor authentication is enabled. Enter your password and current 2FA code below to disable it. If you lost access to your 2FA app, you can also enter one of your recovery codes.<br />Warning: Other devices will remain logged in."));
                        e.Add(new ContainerElement(null,
                        [
                            new Heading("Password:"),
                            new TextBox("Enter your password...", null, "password", TextBoxRole.Password, "Continue('disable')"),
                            new Heading("2FA code / recovery:"),
                            new TextBox("Enter the current code...", null, "code", TextBoxRole.NoSpellcheck, "Continue('disable')")
                        ]));
                        e.Add(new ButtonElementJS("Disable", null, "Continue('disable')"));
                        Presets.AddError(page);
                    }
                    else
                    { //2fa not fully enabled, show option to enable it
                        user.TwoFactor.GenerateTOTP();
                        if (user.TwoFactor.TOTP == null)
                            break;
                        e.Add(new HeadingElement("2FA settings", "Two-factor authentication is disabled. Follow the steps below to enable it.<br />Warning: Other devices will remain logged in."));
                        e.Add(new ContainerElement("Private key:",
                        [
                            new Paragraph("First, scan the QR code using your authenticator app or manually enter the private key below it."),
                            new Image(user.TwoFactor.TOTP.QRImageBase64Src(request.Domain, user.Username), "max-height: 15rem"),
                            new Paragraph("Key: " + user.TwoFactor.TOTP.SecretKey)
                        ]));
                        e.Add(new ContainerElement("Recovery codes:", "Next, copy these recovery codes or download them as a file. They can be used like single-use 2FA codes in case you lose access to your authenticator app, so keep them safe.<br /><br />" + string.Join("<br />", user.TwoFactor.TOTP.Recovery))
                        { Button = new Button("Download", $"/dl{pathPrefix}/2fa-recovery", newTab: true) });
                        e.Add(new ContainerElement("Confirm:",
                        [
                            new Paragraph("Finally, enter your password and the current code shown by your 2FA app."),
                            new Heading("Password:"),
                            new TextBox("Enter your password...", null, "password", TextBoxRole.Password, "Continue('enable')"),
                            new Heading("2FA code:"),
                            new TextBox("Enter the current code...", null, "code", TextBoxRole.NoSpellcheck, "Continue('enable')")
                        ]));
                        e.Add(new ButtonElementJS("Enable", null, "Continue('enable')"));
                        Presets.AddError(page);
                    }
                }
                break;
            case "/username":
                page.Title = "Username settings";
                page.Scripts.Add(new Script($"{pathPrefix}/settings/username.js"));
                e.Add(new HeadingElement("Username settings", "Warning: Other devices will remain logged in.<br />Current: " + user.Username));
                e.Add(new ContainerElement("New username:", new TextBox("Enter a username...", user.Username, "username", TextBoxRole.Username, "Continue()")));
                request.AddAuthElements();
                e.Add(new ButtonElementJS("Change", null, "Continue()"));
                Presets.AddError(page);
                break;
            case "/password":
                page.Title = "Password settings";
                page.Scripts.Add(new Script($"{pathPrefix}/settings/password.js"));
                e.Add(new HeadingElement("Password settings", "Warning: Other devices will remain logged in."));
                if (user.Settings.ContainsKey("PasswordReset"))
                    e.Add(new ContainerElement("Warning", "A password reset has been requested and a corresponding link has been sent to your email address.", "red")
                    { Button = new ButtonJS("Cancel", "Cancel()", "red") });
                e.Add(new ContainerElement(null,
                [
                    new Heading("New password:"),
                    new TextBox("Enter a password...", null, "password1", TextBoxRole.NewPassword, "Continue()"),
                    new Heading("Confirm password:"),
                    new TextBox("Enter the password again...", null, "password2", TextBoxRole.NewPassword, "Continue()")
                ]));
                request.AddAuthElements();
                e.Add(new ButtonElementJS("Change", null, "Continue()", id: "continueButton"));
                Presets.AddError(page);
                break;
            case "/email":
                page.Title = "Email settings";
                if (user.Settings.TryGetValue("EmailChange", out var settingRaw))
                {
                    if (request.Query.TryGet("action") == "cancel")
                    {
                        user.Settings.Delete("EmailChange");
                        request.Redirect($"{pathPrefix}/settings/email");
                        break;
                    }
                    string[] setting = settingRaw.Split('&');
                    string mail = HttpUtility.UrlDecode(setting[0]);
                    string code = setting[1];
                    page.Scripts.Add(new Script($"{pathPrefix}/settings/email-verify.js"));
                    e.Add(new HeadingElement("Email settings", $"You requested to change your email to '{mail}'. Please enter the verification code provided in the email we sent to that address here.")
                    { Button = new Button("Cancel", $"{pathPrefix}/settings/email?action=cancel", "red") });
                    e.Add(new ContainerElement("Verification code", new TextBox("Enter the code...", null, "code", TextBoxRole.NoSpellcheck, "Continue()", autofocus: true))
                    { Button = new ButtonJS("Send again", "Resend()") });
                    e.Add(new ButtonElementJS("Change", null, "Continue()"));
                    Presets.AddError(page);
                }
                else
                {
                    page.Scripts.Add(new Script($"{pathPrefix}/settings/email.js"));
                    e.Add(new HeadingElement("Email settings", $"Current: {user.MailAddress}"));
                    e.Add(new ContainerElement("New email:", new TextBox("Enter the email address...", user.MailAddress, "email", TextBoxRole.Email, "Continue()")));
                    request.AddAuthElements();
                    e.Add(new ButtonElementJS("Continue", null, "Continue()"));
                    Presets.AddError(page);
                }
                break;
            case "/delete":
                page.Title = "Delete account";
                page.Scripts.Add(new Script($"{pathPrefix}/settings/delete.js"));
                e.Add(new HeadingElement("Delete account",
                [
                    new Paragraph("We're very sad to see you go! If you're leaving because you've been experiencing issues, please let us know and we'll try our best to fix it. The goal of this project is to make your experience as nice as possible."),
                    new Paragraph($"If you really want to delete your account, enter your password{(user.TwoFactor.TOTPEnabled()?" and 2FA code":"")} below.")
                ]));
                request.AddAuthElements();
                e.Add(new ButtonElementJS("Delete account :(", null, "Continue()"));
                Presets.AddError(page);
                break;
            default:
                request.Status = 404;
                break;
        }
    }
}