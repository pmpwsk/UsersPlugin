namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public override async Task Handle(Request req)
    {
        switch (Parsers.GetFirstSegment(req.Path, out _))
        {
            case "settings":
                await Settings(req);
                break;
            case "recovery":
                await Recovery(req);
                break;
            case "users":
                await Users(req);
                break;
            default:
                await Other(req);
                break;
        }
    }
}