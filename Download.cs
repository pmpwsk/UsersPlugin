using System.Text;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public override async Task Handle(DownloadRequest request, string path, string pathPrefix)
    {
        switch (path)
        {
            case "/2fa-recovery":
                if (!request.LoggedIn)
                    request.Status = 403;
                else if (!request.User.TwoFactor.TOTPEnabled(out var totp))
                    request.Status = 404;
                else await request.SendBytes(Encoding.UTF8.GetBytes(string.Join('\n', totp.Recovery)), $"2FA Recovery Codes ({request.Domain}).txt");
                break;
            default:
                request.Status = 404;
                break;
        }
    }
}