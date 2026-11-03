using uwap.WebFramework.Localization;
using uwap.WebFramework.Responses.Base;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    [Endpoint("/settings/apps")]
    protected static Page HandleAppsSettings(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        var page = new Page(req, true, PluginTexts.Applications);
        page.Sidebar.Items.ReplaceAll(SettingsSidebar);
        page.Sections.Add(new(
            PluginTexts.Applications,
            [
                new Subsection(
                    null,
                    [
                        ..req.User.Auth.Where(a => a.Value.LimitedToPaths != null).ToElements(
                            () => [ new Paragraph(PluginTexts.ApplicationsIntro) ],
                            a => [ new BigServerActionButton(
                                a.Value.FriendlyName ?? CommonTexts.Unknown,
                                $"{PluginTexts.AllowedPaths}: {(a.Value.LimitedToPaths != null ? a.Value.LimitedToPaths.Count : PluginTexts.All)}",
                                _ => DialogBuilder.Open(
                                    page,
                                    a.Value.FriendlyName ?? CommonTexts.Unknown,
                                    [
                                        .. a.Value.LimitedToPaths != null
                                            ? (IEnumerable<AbstractElement>) [
                                                new Paragraph($"{PluginTexts.AllowedPaths}:"),
                                                new BulletList(a.Value.LimitedToPaths.Select(p => new ListItem(p)))
                                            ] : [
                                                new Paragraph($"{PluginTexts.AllowedPaths}: {PluginTexts.All}"),
                                            ],
                                        new Row(
                                            new ServerSubmitButton(CommonTexts.Delete, _ => DialogBuilder.Open(
                                                page,
                                                a.Value.FriendlyName ?? CommonTexts.Unknown,
                                                [
                                                    new Paragraph(PluginTexts.ReallyRemoveApplication),
                                                    new Row(
                                                        new ContinueButton(),
                                                        new DialogBackButton(page)
                                                    )
                                                ],
                                                async actionReq =>
                                                {
                                                    await actionReq.UserTable.DeleteTokenAsync(actionReq.User.Id, a.Key);
                                                    await page.ReloadAsync();
                                                }
                                            )),
                                            new DialogBackButton(page)
                                        )
                                    ],
                                    _ => {}
                                )
                            ) ],
                            () => [ new Paragraph(PluginTexts.NoApplications) ]
                        )
                    ]
                )
            ]
        ));
        return page;
    }
}