using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static async Task<IResponse> HandleLogout(Request req)
    {
        req.ForceGET();
        await req.UserTable.LogoutAsync(req);
        return new RedirectResponse(req.RedirectUrl);
    }
}