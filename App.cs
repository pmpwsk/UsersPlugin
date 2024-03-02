using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public override Task Handle(AppRequest req, string path, string pathPrefix)
    {
        Presets.CreatePage(req, "Account", out var page, out _);
        Presets.Navigation(req, page);
        switch (Parsers.GetFirstSegment(path, out string rest))
        {
            case "settings":
                Settings(req, rest, pathPrefix);
                break;
            case "recovery":
                Recovery(req, rest, pathPrefix);
                break;
            case "users":
                Users(req, rest, pathPrefix);
                break;
            default:
                Other(req, path, pathPrefix);
                break;
        }
        return Task.CompletedTask;
    }

    private static bool AlreadyLoggedIn(AppRequest request)
    {
        if (!request.HasUser)
            return false;
        else request.Redirect(request.RedirectUrl);
        return true;
    }

    private static bool NotLoggedIn(AppRequest request)
    {
        if (request.LoggedIn)
            return false;
        else request.RedirectToLogin();
        return true;
    }
}