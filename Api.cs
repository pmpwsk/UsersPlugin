namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public override async Task Handle(ApiRequest req, string path, string pathPrefix)
    {
        switch (Parsers.GetFirstSegment(path, out string rest))
        {
            case "settings":
                await Settings(req, rest, pathPrefix);
                break;
            case "recovery":
                await Recovery(req, rest, pathPrefix);
                break;
            case "users":
                await Users(req, rest, pathPrefix);
                break;
            default:
                await Other(req, path, pathPrefix);
                break;
        }
    }

    private async static Task<bool> AlreadyLoggedIn(ApiRequest request)
    {
        if (!request.HasUser) return false;
        await request.Write("Already logged in.");
        return true;
    }

    private async static Task<bool> NotLoggedIn(ApiRequest request)
    {
        if (request.LoggedIn) return false;
        await request.Write("Not logged in.");
        return true;
    }
}