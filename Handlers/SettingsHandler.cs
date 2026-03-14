using System.Text;
using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private async Task<IResponse> OldHandleSettings(Request req)
    {
        switch (req.Path)
        {
            // MANAGE APPS
            case "/settings/apps":
            { req.ForceGET(); req.ForceLogin();
                Presets.CreatePage(req, "Applications", out var page, out var e);
                page.Navigation.Add(new Elements.Button("Back", "../settings", "right"));
                page.Scripts.Add(new Elements.Script("apps.js"));
                e.Add(new Elements.HeadingElement("Applications", "These are the applications that have partial access to your account."));
                page.AddError();
                int index = 0;
                bool foundAny = false;
                foreach (var kv in req.User.Auth)
                {
                    if (kv.Value.LimitedToPaths != null)
                    {
                        foundAny = true;
                        e.Add(new Elements.ContainerElement(kv.Value.FriendlyName,
                        [
                            new Elements.Paragraph($"Expires: {kv.Value.Expires} UTC"),
                            new Elements.BulletList(kv.Value.LimitedToPaths)
                        ]) { Button = new Elements.ButtonJS("Remove", $"Remove('{index}','{HttpUtility.UrlEncode(kv.Value.FriendlyName)}','{kv.Value.Expires.Ticks}')", "red") });
                    }
                    index++;
                }
                if (!foundAny)
                    e.Add(new Elements.ContainerElement("No applications!", "", classes: "red"));
                return new LegacyPageResponse(page, req);
            }

            case "/settings/apps/remove":
            { req.ForcePOST(); req.ForceLogin(false);
                if (req.Query.TryGetValue("index", out int index) && index >= 0 && req.Query.TryGetValue("name", out var name) && req.Query.TryGetValue("expires", out long ticks))
                {
                    var kv = req.User.Auth.ElementAtOrDefault(index);
                    if (kv.Equals(default(KeyValuePair<string, AuthTokenData>)))
                        return StatusResponse.NotFound;
                    else if (kv.Value.FriendlyName == name && kv.Value.Expires.Ticks == ticks)
                    {
                        await req.UserTable.DeleteTokenAsync(req.User.Id, kv.Key);
                        return StatusResponse.Success;
                    }
                    else
                        return StatusResponse.NotFound;
                }
                else
                    return StatusResponse.BadRequest;
            }




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