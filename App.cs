using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public override Task Handle(AppRequest request, string path, string pathPrefix)
    {
        Presets.CreatePage(request, "Account", out var page, out var e);
        Presets.Navigation(request, page);
        switch (Parsers.GetFirstSegment(path, out string rest))
        {
            case "settings":
                Settings(request, rest, pathPrefix);
                break;
            case "recovery":
                Recovery(request, rest, pathPrefix);
                break;
            case "users":
                Users(request, rest, pathPrefix);
                break;
            default:
                Other(request, path, pathPrefix);
                break;
        }
        return Task.CompletedTask;
    }

    private static bool AlreadyLoggedIn(AppRequest request)
    {
        if (request.User == null) return false;
        else request.Redirect(request.RedirectUrl);
        return true;
    }

    private static bool NotLoggedIn(AppRequest request)
    {
        if (request.LoginState == LoginState.LoggedIn) return false;
        else request.RedirectToLogin();
        return true;
    }
}