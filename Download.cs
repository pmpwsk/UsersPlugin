using System.Text;
using uwap.WebFramework.Accounts;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public override async Task Handle(DownloadRequest request, string path, string pathPrefix)
    {
        switch (path)
        {
            case "/2fa-recovery":
                if (request.User == null || !request.LoggedIn) request.Status = 403;
                else
                {
                    User user = request.User;
                    if (user.TwoFactor == null) request.Status = 404;
                    else if (user.TwoFactor.Verified) request.Status = 403;
                    else
                    {
                        await request.SendBytes(Encoding.UTF8.GetBytes(string.Join('\n', user.TwoFactor.Recovery)), $"2FA Recovery Codes ({request.Domain}).txt");
                    }
                }
                break;
            default:
                request.Status = 404;
                break;
        }
    }
}