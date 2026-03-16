using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static async Task<IResponse> HandleLogout(Request req)
    {
        if (req.Method == "POST")
        {
            await req.UserTable.LogoutAsync(req);
            return StatusResponse.Success;
        }
        
        req.ForceGET(); req.ForceLogin();
        return new Page(
            req, true,
            "Logout",
            MainSidebar(req),
            [
                new Section(
                    "Logout",
                    [
                        new Subsection(
                            null,
                            [
                                new Paragraph("Are you sure you want to log out?."),
                                new BigServerActionButton(
                                    "Yes, log me out",
                                    [],
                                    async actionReq =>
                                    {
                                        await actionReq.UserTable.LogoutAsync(req);
                                        return new Navigate("/");
                                    }
                                ),
                                new BigLinkButton("Back to account", [], ".")
                            ]
                        )
                    ]
                )
            ]
        );
    }
}