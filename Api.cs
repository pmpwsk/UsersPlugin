using System.Text;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public override async Task Handle(ApiRequest req, string path, string pathPrefix)
    {
        switch (Parsers.GetFirstSegment(path, out string rest))
        {
            case "settings":
                await Settings(req, rest, pathPrefix);
                break;
            case "recovery":
                await Recovery(req, rest, pathPrefix);
                break;
            case "users":
                await Users(req, rest, pathPrefix);
                break;
            case "theme.css":
                if (rest == "")
                {
                    ThemeFromQuery(req.Context.Request.QueryString.Value??"", out string font, out string? fontMono, out string background, out string accent, out string design);

                    //save domain + timestamp
                    string domain = req.Domain;
                    string timestamp = Timestamp(font, fontMono, background, accent, design);

                    //content type
                    if (Server.Config.MimeTypes.TryGetValue(".css", out string? type)) req.Context.Response.ContentType = type;

                    //browser cache
                    if (Server.Config.BrowserCacheMaxAge.TryGetValue(".css", out int maxAge))
                    {
                        if (maxAge == 0) req.Context.Response.Headers.CacheControl = "no-cache, private";
                        else
                        {
                            if (!req.Context.Response.Headers.ContainsKey("Cache-Control"))
                                req.Context.Response.Headers.CacheControl = "public, max-age=" + maxAge;
                            else req.Context.Response.Headers.CacheControl = "public, max-age=" + maxAge;
                            try
                            {
                                if (req.Context.Request.Headers.TryGetValue("If-None-Match", out var oldTag) && oldTag == timestamp)
                                {
                                    req.Context.Response.StatusCode = 304;
                                    if (Server.Config.FileCorsDomain != null)
                                        req.Context.Response.Headers.AccessControlAllowOrigin = Server.Config.FileCorsDomain;
                                    break; //browser already has the current version
                                }
                                else req.Context.Response.Headers.ETag = timestamp;
                            }
                            catch { }
                        }
                    }

                    //send
                    await req.SendBytes(
                    [
                        ..Encoding.UTF8.GetBytes($"/* Font declaration: {font} */\n\n"),
                        ..GetFile($"/theme/f/{font}.css", pathPrefix, domain) ?? [],
                        ..(fontMono == null ? (byte[])[] :
                        [
                            ..Encoding.UTF8.GetBytes($"\n\n\n/* Font declaration: {fontMono} */\n\n"),
                            ..GetFile($"/theme/f/{fontMono}.css", pathPrefix, domain) ?? [],
                        ]),
                        ..Encoding.UTF8.GetBytes($"\n\n\n/* Font: {font} */\n\n:root {{\n\t--font-family: '{font}';\n\t--font-code: '{fontMono??font}';\n}}\n"),
                        ..Encoding.UTF8.GetBytes($"\n\n\n/* Accent: {AccentName(accent, background)} */\n\n"),
                        ..GetFile($"/theme/a/{AccentName(accent, background)}.css", pathPrefix, domain) ?? [],
                        ..Encoding.UTF8.GetBytes($"\n\n\n/* Background: {background} */\n\n"),
                        ..GetFile($"/theme/b/{background}.css", pathPrefix, domain) ?? [],
                        ..Encoding.UTF8.GetBytes($"\n\n\n/* Design: {design} */\n\n"),
                        ..GetFile($"/theme/d/{design}.css", pathPrefix, domain) ?? []
                    ]);
                }
                else req.Status = 404;
                break;
            default:
                await Other(req, path, pathPrefix);
                break;
        }
    }

    private async static Task<bool> AlreadyLoggedIn(ApiRequest request)
    {
        if (!request.HasUser) return false;
        await request.Write("Already logged in.");
        return true;
    }

    private async static Task<bool> NotLoggedIn(ApiRequest request)
    {
        if (request.LoggedIn) return false;
        await request.Write("Not logged in.");
        return true;
    }
}