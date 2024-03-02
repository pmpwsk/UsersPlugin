using System.Text;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public override async Task Handle(DownloadRequest req, string path, string pathPrefix)
    {
        switch (path)
        {
            case "/2fa-recovery":
                if (!req.LoggedIn)
                    req.Status = 403;
                else if (req.User.TwoFactor.TOTP == null || req.User.TwoFactor.TOTPEnabled(out _))
                    req.Status = 404;
                else await req.SendBytes(Encoding.UTF8.GetBytes(string.Join('\n', req.User.TwoFactor.TOTP.Recovery)), $"2FA Recovery Codes ({req.Domain}).txt");
                break;
            default:
                req.Status = 404;
                break;
        }
    }
}