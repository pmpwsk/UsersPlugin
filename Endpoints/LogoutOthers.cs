using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/logout-others")]
    protected static IResponse HandleLogoutOthers(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true, "Logout others");
        page.Sidebar.Items.ReplaceAll(MainSidebar(req));
        page.Sections.Add(new(
            "Logout others",
            [
                new Subsection(
                    null,
                    [
                        new Paragraph("Are you sure you want to log out all other browsers and all applications with partial access?."),
                        new BigServerActionButton(
                            "Yes, log them out",
                            [],
                            async actionReq =>
                            {
                                await actionReq.UserTable.LogoutOthersAsync(actionReq);
                                page.Navigate(".");
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