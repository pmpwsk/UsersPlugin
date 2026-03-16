using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.Base;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static Page HandleAppsSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true, "Applications");
        page.Sidebar.Items.ReplaceAll(SettingsSidebar);
        page.Sections.Add(new(
            "Applications",
            [
                new Subsection(
                    null,
                    [
                        ..req.User.Auth.Where(a => a.Value.LimitedToPaths != null).ToElements(
                            () => [ new Paragraph("These are the applications that have partial access to your account.") ],
                            a => [ new BigServerActionButton(
                                a.Value.FriendlyName ?? "Unknown",
                                [ $"Expires: {a.Value.Expires} UTC" ],
                                _ => Task.FromResult<IActionResponse>(page.DynamicDialogAction(
                                    a.Value.FriendlyName ?? "Unknown",
                                    [
                                        new Paragraph($"Expires: {a.Value.Expires} UTC"),
                                        new BulletList(a.Value.LimitedToPaths != null ? a.Value.LimitedToPaths.Select(p => new ListItem(p)) : []),
                                        new CustomElement(
                                            "div",
                                            [
                                                new ServerActionButton(new("bi bi-trash", "Delete"), _ => Task.FromResult<IActionResponse>(page.DynamicDialogAction(
                                                    a.Value.FriendlyName ?? "Unknown",
                                                    [
                                                        new Paragraph("Do you really want to remove this application's partial access to your account?"),
                                                        new CustomElement(
                                                            "div",
                                                            [
                                                                new ServerActionButton(new("bi bi-arrow-return-right", "Continue"), async actionReq =>
                                                                {
                                                                    await actionReq.UserTable.DeleteTokenAsync(actionReq.User.Id, a.Key);
                                                                    return new Reload();
                                                                }),
                                                                new ServerActionButton("Cancel", _ =>
                                                                {
                                                                    page.CloseDynamicDialog();
                                                                    return Task.FromResult<IActionResponse>(new Nothing());
                                                                })
                                                            ]
                                                        ) { Class = "wf-row" }
                                                    ]
                                                ))),
                                                new ServerActionButton("Close", _ =>
                                                {
                                                    page.CloseDynamicDialog();
                                                    return Task.FromResult<IActionResponse>(new Nothing());
                                                })
                                            ]
                                        ) { Class = "wf-row" }
                                    ]
                                ))
                            ) ],
                            () => [ new Paragraph("There are currently no applications that have partial access to your account.") ]
                        )
                    ]
                )
            ]
        ));
        return page;
    }
}