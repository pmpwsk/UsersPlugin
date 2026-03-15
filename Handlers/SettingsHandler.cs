using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private async Task<IResponse> OldHandleSettings(Request req)
    {
        switch (req.Path)
        {
            // THEME SETTINGS
            case "/settings/theme":
            { req.ForceGET(); req.ForceLogin();
                Presets.CreatePage(req, "Theme settings", out var page, out var e);
                page.Navigation.Add(new Elements.Button("Back", "../settings", "right"));
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Elements.Script("theme.js"));
                e.Add(new Elements.HeadingElement("Theme settings"));
                page.AddError();
                ThemeFromQuery((req.LoggedIn && req.User.Settings.TryGetValue("Theme", out string? theme)) ? theme : "default", out string font, out string? _, out string background, out string accent, out string design);
                e.Add(new Elements.ContainerElement("Background", new Elements.Selector("background", background, [..Backgrounds]) { OnChange = "Save()" }));
                e.Add(new Elements.ContainerElement("Accent", new Elements.Selector("accent", accent, [..Accents]) { OnChange = "Save()" }));
                e.Add(new Elements.ContainerElement("Design", new Elements.Selector("design", design, [..Designs]) { OnChange = "Save()" }));
                e.Add(new Elements.ContainerElement("Font", new Elements.Selector("font", font, [..Fonts]) { OnChange = "Save()" }));
                return new LegacyPageResponse(page, req);
            }
            
            case "/settings/theme/set":
            { req.ForcePOST(); req.ForceLogin(false);
                if (!ThemeFromQuery(req.Query.FullString, out string font, out _, out string background, out string accent, out string design))
                    return StatusResponse.BadRequest;
                await req.UserTable.SetSettingAsync(req.User.Id, "Theme", $"?f={font}&b={background}&a={accent}&d={design}");
                return StatusResponse.Success;
            }




            // 404
            default:
                return StatusResponse.NotFound;
        }
    }
}