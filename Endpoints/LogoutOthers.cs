using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/logout-others")]
    protected static IResponse HandleLogoutOthers(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true, PluginTexts.LogoutOthers);
        page.Sidebar.Items.ReplaceAll(MainSidebar(req));
        page.Sections.Add(new(
            PluginTexts.LogoutOthers,
            [
                new Subsection(
                    null,
                    [
                        new Paragraph(PluginTexts.LogoutOthersIntro),
                        new BigServerActionButton(
                            CommonTexts.Continue,
                            PluginTexts.ThisStaysLoggedIn,
                            async actionReq =>
                            {
                                await actionReq.UserTable.LogoutOthersAsync(actionReq);
                                page.Navigate(".");
                            }
                        ),
                        new BigLinkButton(
                            CommonTexts.Back,
                            PluginTexts.AllStayLoggedIn,
                            "."
                        )
                    ]
                )
            ]
        ));
        return page;
    }
}