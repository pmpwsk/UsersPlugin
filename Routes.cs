using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public override async Task<IResponse> HandleAsync(Request req)
        => req.Path switch
        {
            "/" => HandleMenu(req),
            "/2fa" => HandleTwoFactor(req),
            "/get-username" => HandleGetUsername(req),
            "/login" => HandleLogin(req),
            "/logout" => await HandleLogout(req),
            "/logout-others" => HandleLogoutOthers(req),
            "/register" => HandleRegister(req),
            "/verify" => HandleVerify(req),
            "/verify-change" => HandleVerifyChange(req),
            "/verify-link" => await HandleVerifyLink(req),
            
            "/recovery" => HandleRecovery(req),
            "/recovery/2fa" => HandleTwoFactorRecovery(req),
            "/recovery/password" => HandlePasswordRecovery(req),
            "/recovery/password-set" => await HandleSetPasswordRecovery(req),
            "/recovery/username" => HandleUsernameRecovery(req),
            
            "/settings" => HandleSettings(req),
            "/settings/2fa" => await HandleTwoFactorSettings(req),
            "/settings/2fa-codes" => HandleTwoFactorSettingsCodes(req),
            "/settings/apps" => HandleAppsSettings(req),
            "/settings/delete" => HandleDeleteSettings(req),
            "/settings/email" => HandleEmailSettings(req),
            "/settings/password" => HandlePasswordSettings(req),
            "/settings/username" => HandleUsernameSettings(req),
            
            _ => await (Parsers.GetFirstSegment(req.Path, out _) switch
            {
                "settings" => OldHandleSettings(req),
                "users" => OldHandleUsers(req),
                _ => OldHandleOther(req)
            })
        };
}