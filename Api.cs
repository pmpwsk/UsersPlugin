using uwap.WebFramework.Accounts;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public override async Task Handle(ApiRequest request, string path, string pathPrefix)
    {
        switch (Parsers.GetFirstSegment(path, out string rest))
        {
            case "settings":
                await Settings(request, rest, pathPrefix);
                break;
            case "recovery":
                await Recovery(request, rest, pathPrefix);
                break;
            case "users":
                await Users(request, rest, pathPrefix);
                break;
            default:
                await Other(request, path, pathPrefix);
                break;
        }
    }

    private async static Task<bool> AlreadyLoggedIn(ApiRequest request)
    {
        if (request.User == null) return false;
        else await request.Write("Already logged in.");
        return true;
    }

    private async static Task<bool> NotLoggedIn(ApiRequest request)
    {
        if (request.LoginState == LoginState.LoggedIn) return false;
        else await request.Write("Not logged in.");
        return true;
    }
}