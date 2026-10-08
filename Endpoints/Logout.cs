using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/logout")]
    protected static async Task<IResponse> HandleLogout(Request req)
    {
        if (req.Method == "POST")
        {
            await req.UserTable.LogoutAsync(req);
            return StatusResponse.Success;
        }
        
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true, "Logout");
        page.Sidebar.Items.ReplaceAll(MainSidebar(req));
        page.Sections.Add(new(
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
                                await actionReq.UserTable.LogoutAsync(actionReq);
                                page.Navigate("/");
                            }
                        ),
                        new BigLinkButton("Back to account", [], ".")
                    ]
                )
            ]
        ));
        return page;
    }
}