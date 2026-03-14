using System.Text;
using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private async Task<IResponse> OldHandleOther(Request req)
    {
        switch (req.Path)
        {
            // AUTH REQUEST (LIMITED TOKEN)
            case "/auth-request":
            { req.ForceGET(); req.ForceLogin();
                Presets.CreatePage(req, "Auth request", out var page, out var e);
                if (req.Query.TryGetValue("name", out var name) && name != "" && name == name.HtmlSafe() && req.Query.TryGetValue("yes", out var yes) && req.Query.TryGetValue("no", out var no) && req.Query.TryGetValue("allowed", out var limitedToPathsEncoded))
                {
                    var limitedToPaths = limitedToPathsEncoded.Split(',').Select(x => HttpUtility.UrlDecode(x).HtmlSafe()).ToList();
                    if (limitedToPaths.Contains(""))
                        return StatusResponse.BadRequest;
                    page.Scripts.Add(new Elements.Script("auth-request.js"));
                    string? backgroundDomain = req.Query.TryGetValue("background", out var b) && b.SplitAtFirst("://", out var bProto, out b) && (bProto == "http" || bProto == "https") && b.SplitAtFirst('/', out b, out _) ? b : null;
                    e.Add(new Elements.LargeContainerElement("Authentication request",
                    [
                        new Elements.Paragraph($"The application \"{name}\" would like to authenticate using {req.Domain}."),
                        new Elements.Paragraph("It will only get access to the following addresses:"),
                        new Elements.BulletList(limitedToPaths)
                    ]));
                    page.AddError();
                    e.Add(new Elements.ButtonElementJS("Allow", $"{(backgroundDomain == null ? "O" : $"Gives a token to \"{backgroundDomain.HtmlSafe()}\" and o")}pens:<br/>{yes.Before('?').HtmlSafe()}", "Allow()", "green") {Unsafe = true});
                    e.Add(new Elements.ButtonElement("Reject", $"Opens:<br/>{no.Before('?').HtmlSafe()}", no.HtmlSafe(), "red") {Unsafe = true});
                    e.Add(new Elements.ContainerElement(null,
                    [
                        "Addresses ending with * indicate that the application can access any address starting with the part before the *.",
                        "Closing this page will also reject the request, but the application won't be opened in order for it to know that it was rejected.",
                        "You can manage applications you have given access to in your account settings.",
                        "Logging out all other devices from your account menu also logs you out of any applications you have given access to.",
                        $"In order to be able to log themselves out, applications automatically get access to \"{req.PluginPathPrefix}/logout\" as well."
                    ]));
                    return new LegacyPageResponse(page, req);
                }
                else
                    return StatusResponse.BadRequest;
            }
                
            case "/auth-request/generate-limited-token":
            { req.ForcePOST(); req.ForceLogin(false);
                if (req.Query.TryGetValue("name", out var name) && name != "" && name == name.HtmlSafe() && req.Query.TryGetValue("return", out var returnAddress) && req.Query.TryGetValue("allowed", out var limitedToPathsEncoded))
                {
                    var limitedToPaths =
                    ((IEnumerable<string>)[
                        ..limitedToPathsEncoded.Split(',').Select(x => HttpUtility.UrlDecode(x).HtmlSafe()),
                        $"{req.PluginPathPrefix}/logout"
                    ]).ToList().AsReadOnly();
                    if (limitedToPaths.Contains(""))
                        return StatusResponse.BadRequest;
                    string token = await req.UserTable.AddNewLimitedTokenAsync(req.User.Id, name, limitedToPaths);
                    AccountManager.GenerateAuthTokenCookieOptions(out var expires, out var sameSite, out var domain, req);
                    await Presets.WarningMailAsync(req, req.User, $"App '{name}' was granted access", $"The app '{name}' has just been granted limited access to your account. You can view and manage apps with access in your account settings.");
                    return new TextResponse(returnAddress
                        .Replace("[TOKEN]", req.User.Id + token)
                        .Replace("[EXPIRES]", expires.Ticks.ToString())
                        .Replace("[SAMESITE]", sameSite.ToString())
                        .Replace("[DOMAIN]", domain));
                }
                else
                    return StatusResponse.BadRequest;
            }




            // USER STYLE
            case "/theme.css":
            { req.ForceGET();
                ThemeFromQuery(req.Query.FullString, out string font, out string? fontMono, out string background, out string accent, out string design);
                string domain = req.Domain;
                string timestamp = Timestamp(font, fontMono, background, accent, design);
                return new ByteArrayResponse(
                [
                    ..Encoding.UTF8.GetBytes($"/* Font declaration: {font} */\n\n"),
                    ..GetFile($"/theme/f/{font}.css", req.PluginPathPrefix, domain) ?? [],
                    ..(fontMono == null ? (byte[])[] :
                    [
                        ..Encoding.UTF8.GetBytes($"\n\n\n/* Font declaration: {fontMono} */\n\n"),
                        ..GetFile($"/theme/f/{fontMono}.css", req.PluginPathPrefix, domain) ?? [],
                    ]),
                    ..Encoding.UTF8.GetBytes($"\n\n\n/* Font: {font} */\n\n:root {{\n\t--font-family: '{font}';\n\t--font-code: '{fontMono??font}';\n}}\n"),
                    ..Encoding.UTF8.GetBytes($"\n\n\n/* Accent: {AccentName(accent, background)} */\n\n"),
                    ..GetFile($"/theme/a/{AccentName(accent, background)}.css", req.PluginPathPrefix, domain) ?? [],
                    ..Encoding.UTF8.GetBytes($"\n\n\n/* Background: {background} */\n\n"),
                    ..GetFile($"/theme/b/{background}.css", req.PluginPathPrefix, domain) ?? [],
                    ..Encoding.UTF8.GetBytes($"\n\n\n/* base.css */\n\n"),
                    ..GetFile($"/theme/base.css", req.PluginPathPrefix, domain) ?? [],
                    ..Encoding.UTF8.GetBytes($"\n\n\n/* Design: {design} */\n\n"),
                    ..GetFile($"/theme/d/{design}.css", req.PluginPathPrefix, domain) ?? []
                ], ".css", true, timestamp);
            }




            // 404
            default:
                return StatusResponse.NotFound;
        }
    }
}