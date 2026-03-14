using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static IResponse HandleGetUsername(Request req)
    {
        req.ForceGET();
        if (req.LoggedIn)
            return new TextResponse(req.User.Username);
        else
            return StatusResponse.Forbidden;
    }
}