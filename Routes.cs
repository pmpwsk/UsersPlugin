using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public override async Task<IResponse> HandleOtherAsync(Request req)
        => await (Parsers.GetFirstSegment(req.Path, out _) switch
        {
            "settings" => OldHandleSettings(req),
            "users" => OldHandleUsers(req),
            _ => OldHandleOther(req)
        });
}