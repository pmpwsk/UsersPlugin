using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public override Task<IResponse> HandleAsync(Request req)
        => Parsers.GetFirstSegment(req.Path, out _) switch
        {
            "settings" => HandleSettings(req),
            "recovery" => HandleRecovery(req),
            "users" => HandleUsers(req),
            _ => HandleOther(req)
        };
}