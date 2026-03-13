using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static async Task<IResponse> HandleRecovery(Request req)
    {
        switch (req.Path)
        {
            // MAIN USER MANAGEMENT PAGE
            case "/recovery":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        return new RedirectResponse(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        return new RedirectResponse("2fa" + req.CurrentRedirectQuery);
                    case LoginState.NeedsMailVerification:
                        return new RedirectResponse("verify" + req.CurrentRedirectQuery);
                }
                var page = new Page(req, false);
                page.Title = "Recovery";
                page.Sections.Add(new(
                    "Recovery",
                    [
                        new Subsection(
                            null,
                            [
                                new Paragraph("Here are some options to recover your account. If you still can't log in, please contact our support."),
                                new BigLinkButton(new("bi bi-person", "Username"), ["Receive an email containing your username."], $"recovery/username{req.CurrentRedirectQuery}"),
                                new BigLinkButton(new("bi bi-key", "Password"), ["Receive an email to reset your password."], $"recovery/password{req.CurrentRedirectQuery}"),
                                new BigLinkButton(new("bi bi-lock", "Two-factor authentication"), ["Information about 2FA recovery."], $"recovery/2fa{req.CurrentRedirectQuery}"),
                                Presets.CreateSupportButton(req)
                            ]
                        )
                    ]
                ));
                return page;
            }




            // REQUEST USERNAME
            case "/recovery/username":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        return new RedirectResponse(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        return new RedirectResponse("../2fa" + req.CurrentRedirectQuery);
                    case LoginState.NeedsMailVerification:
                        return new RedirectResponse("../verify" + req.CurrentRedirectQuery);
                }
                var page = new Page(req, true);
                page.Title = "Username recovery";
                var emailInput = new TextBox("email", "Enter your email address...", null, TextBoxRole.Email) { Autofocus = true };
                page.Sections.Add(new(
                    "Username recovery",
                    [
                        new ServerForm(
                            null,
                            async actionReq =>
                            {
                                if (!actionReq.HasUser)
                                {
                                    if (emailInput.IsEmpty)
                                        return page.DynamicErrorAction("Please enter your email address.");
            
                                    User? user = await actionReq.UserTable.FindByMailAddressAsync(emailInput.Value);
                                    if (user == null)
                                    {
                                        AccountManager.ReportFailedAuth(actionReq);
                                        return page.DynamicErrorAction("This email address isn't associated with any account.");
                                    }
                                    await Presets.WarningMailAsync(actionReq, user, "Username recovery", $"You requested your username, it is: {user.Username}");
                                }
        
                                return new Navigate("../login" + req.CurrentRedirectQuery);
                            },
                            [
                                new Paragraph("Enter your email address below and you'll receive an email telling you your username."),
                                new Heading3("Email"),
                                emailInput,
                                new SubmitButton(new("bi bi-arrow-return-right", "Continue"))
                            ]
                        )
                    ]
                ));
                return page;
            }
            
            case "/recovery/username/request":
            { req.ForcePOST();
                if (req.HasUser)
                    return new TextResponse("ok");
                var email = req.Query.GetOrThrow("email");
                User? user = await req.UserTable.FindByMailAddressAsync(email);
                if (user == null)
                {
                    AccountManager.ReportFailedAuth(req);
                    return new TextResponse("no");
                }
                await Presets.WarningMailAsync(req, user, "Username recovery", $"You requested your username, it is: {user.Username}");
                return new TextResponse("ok");
            }




            // RESET PASSWORD
            case "/recovery/password":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        return new RedirectResponse(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        return new RedirectResponse("../2fa" + req.CurrentRedirectQuery);
                    case LoginState.NeedsMailVerification:
                        return new RedirectResponse("../verify" + req.CurrentRedirectQuery);
                }
                var page = new Page(req, true);
                page.Title = "Password recovery";
                var emailInput = new TextBox("email", "Enter your email address...", null, TextBoxRole.Email) { Autofocus = true };
                page.Sections.Add(new(
                    "Username recovery",
                    [
                        new ServerForm(
                            null,
                            async actionReq =>
                            {
                                if (!actionReq.HasUser)
                                {
                                    if (emailInput.IsEmpty)
                                        return page.DynamicErrorAction("Please enter your email address.");

                                    User? user = await actionReq.UserTable.FindByMailAddressAsync(emailInput.Value);
                                    if (user == null)
                                    {
                                        AccountManager.ReportFailedAuth(actionReq);
                                        return page.DynamicErrorAction("This email address isn't associated with any account.");
                                    }
                                    string code = Parsers.RandomString(64);
                                    await actionReq.UserTable.SetSettingAsync(user.Id, "PasswordReset", code);
                                    string url = $"{req.PluginPathPrefix}/recovery/password-set?token={user.Id}{code}";
                                    await Presets.WarningMailAsync(actionReq, user, "Password recovery", $"You requested password recovery. Open the following link to reset your password:\n<a href=\"{url}\">{url}</a>\nYou can cancel the password reset from Account > Settings > Password.");
                                }
        
                                return new Navigate("../login" + req.CurrentRedirectQuery);
                            },
                            [
                                new Paragraph("Enter your email address below and you'll receive an email with a link to reset your password."),
                                new Heading3("Email"),
                                emailInput,
                                new SubmitButton(new("bi bi-arrow-return-right", "Continue"))
                            ]
                        )
                    ]
                ));
                return page;
            }
            
            case "/recovery/password-set":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        return new RedirectResponse(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        return new RedirectResponse("../2fa" + req.CurrentRedirectQuery);
                    case LoginState.NeedsMailVerification:
                        return new RedirectResponse("../verify" + req.CurrentRedirectQuery);
                }
                if (!req.Query.TryGetValue("token", out var token) || token.Length != 76)
                    return StatusResponse.BadRequest;
                
                string id = token[..12];
                string code = token[12..];
                var user = await req.UserTable.GetByIdNullableAsync(id);
                if (user == null || user.Settings.TryGet("PasswordReset") != code)
                    return new RedirectResponse("password");
                
                var page = new Page(req, true);
                page.Title = "Password reset";
                var passwordInput1 = new TextBox("password1", "Enter a password...", null, TextBoxRole.NewPassword) { Autofocus = true };
                var passwordInput2 = new TextBox("password1", "Enter the password again...", null, TextBoxRole.NewPassword);
                page.Sections.Add(new(
                    "Username recovery",
                    [
                        new ServerForm(
                            null,
                            async actionReq =>
                            {
                                if (!actionReq.HasUser)
                                {
                                    if (passwordInput1.IsEmpty || passwordInput2.IsEmpty || passwordInput1.Value != passwordInput2.Value)
                                        return page.DynamicErrorAction("Please enter a new password twice.");
        
                                    try
                                    {
                                        await actionReq.UserTable.SetPasswordAsync(user.Id, passwordInput1.Value);
                                        await Presets.WarningMailAsync(actionReq, user, "Password changed", "Your password was just changed by recovery.");
                                        await actionReq.UserTable.DeleteSettingAsync(user.Id, "PasswordReset");
                                    }
                                    catch (Exception ex)
                                    {
                                        return page.DynamicErrorAction(ex.Message);
                                    }
                                }
        
                                return new Navigate("../login" + req.CurrentRedirectQuery);
                            },
                            [
                                new Paragraph("Enter a new password and confirm it below."),
                                new Heading3("New password"),
                                passwordInput1,
                                new Heading3("Confirm password"),
                                passwordInput2,
                                new SubmitButton(new("bi bi-arrow-return-right", "Change"))
                            ]
                        )
                    ]
                ));
                return page;
            }


            
            
            // 2FA RECOVERY INFORMATION
            case "/recovery/2fa":
            { req.ForceGET();
                switch (req.LoginState)
                {
                    case LoginState.LoggedIn:
                        return new RedirectResponse(req.RedirectUrl);
                    case LoginState.Needs2FA:
                        return new RedirectResponse("../2fa" + req.CurrentRedirectQuery);
                    case LoginState.NeedsMailVerification:
                        return new RedirectResponse("../verify" + req.CurrentRedirectQuery);
                }
                var page = new Page(req, false);
                page.Title = "2FA recovery";
                page.Sections.Add(new(
                    "2FA recovery",
                    [
                        new Subsection(
                            null,
                            [
                                new Paragraph("If you've lost access to your 2FA app, you can use one of the recovery codes you were given when you enabled 2FA. If you've lost those as well, please contact our support."),
                                Presets.CreateSupportButton(req)
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
}