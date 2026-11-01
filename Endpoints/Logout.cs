using uwap.WebFramework.Localization;
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
        var page = new Page(req, true, CommonTexts.Logout);
        page.Sidebar.Items.ReplaceAll(MainSidebar(req));
        page.Sections.Add(new(
            CommonTexts.Logout,
            [
                new Subsection(
                    null,
                    [
                        new Paragraph(PluginTexts.LogoutIntro),
                        new BigServerActionButton(
                            CommonTexts.Continue,
                            PluginTexts.OthersStayLoggedIn,
                            async actionReq =>
                            {
                                await actionReq.UserTable.LogoutAsync(actionReq);
                                await page.NavigateAsync("/");
                            }
                        ),
                        new BigLinkButton(
                            CommonTexts.Back,
                            PluginTexts.LogoutNoDescription,
                            "."
                        )
                    ]
                )
            ]
        ));
        return page;
    }
}