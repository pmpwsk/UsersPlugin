using uwap.WebFramework.Responses.Actions;
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
                                _ => page.DynamicDialogActionAsync(
                                    a.Value.FriendlyName ?? "Unknown",
                                    [
                                        new Paragraph($"Expires: {a.Value.Expires} UTC"),
                                        new BulletList(a.Value.LimitedToPaths != null ? a.Value.LimitedToPaths.Select(p => new ListItem(p)) : []),
                                        new Row([
                                            new ServerSubmitButton(new("bi bi-trash", "Delete"), _ => page.DynamicDialogActionAsync(
                                                a.Value.FriendlyName ?? "Unknown",
                                                [
                                                    new Paragraph("Do you really want to remove this application's partial access to your account?"),
                                                    new Row([
                                                        new ContinueButton(),
                                                        new DialogCancelButton(page)
                                                    ])
                                                ],
                                                async actionReq =>
                                                {
                                                    await actionReq.UserTable.DeleteTokenAsync(actionReq.User.Id, a.Key);
                                                    return new Reload();
                                                }
                                            )),
                                            new SubmitButton("Close")
                                        ])
                                    ],
                                    page.DynamicDialogCloseActionHandler
                                )
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