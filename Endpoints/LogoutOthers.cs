using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static async Task<IResponse> HandleLogoutOthers(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        await req.UserTable.LogoutOthersAsync(req);
        return new Page(
            req, false,
            "Logout others",
            [
                new Section(
                    "Success",
                    [
                        new Subsection(
                            null,
                            [
                                new Paragraph("Successfully logged out all other devices and browsers."),
                                new LinkButton("Back to account", ".")
                            ]
                        )
                    ]
                )
            ]
        );
    }
}