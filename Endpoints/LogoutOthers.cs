using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static IResponse HandleLogoutOthers(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        return new Page(
            req, true,
            "Logout others",
            MainSidebar(req),
            [
                new Section(
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
                                        await actionReq.UserTable.LogoutOthersAsync(req);
                                        return new Navigate(".");
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