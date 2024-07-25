namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public override async Task Handle(Request req)
    {
        switch (Parsers.GetFirstSegment(req.Path, out _))
        {
            case "settings":
                await HandleSettings(req);
                break;
            case "recovery":
                await HandleRecovery(req);
                break;
            case "users":
                await HandleUsers(req);
                break;
            default:
                await HandleOther(req);
                break;
        }
    }
}