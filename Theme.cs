using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    private readonly HashSet<string> Fonts = ["ubuntu", "ubuntu-mono", "roboto", "roboto-mono", "special-elite"];
    private readonly HashSet<string> Backgrounds = ["black", "dark", "light", "white", "beige"];
    private readonly HashSet<string> Accents = ["green", "violet", "blue", "red", "none"];
    private readonly HashSet<string> Designs = ["shadows", "layers", "flat", "no-css"];

    public string DefaultFont = "roboto";
    public string DefaultBackground = "dark";
    public string DefaultAccent = "blue";
    public string DefaultDesign = "layers";


    public Style GetStyle(Request req, out string fontUrl, string pathPrefix)
    {
        ThemeFromQuery((req.LoggedIn && req.User.Settings.TryGetValue("Theme", out string? theme)) ? theme : "default", out string font, out string? fontMono, out string background, out string accent, out string design);
        fontUrl = $"{pathPrefix}/fonts/{font}.woff2";
        return new Style($"{pathPrefix}/theme.css?f={font}&b={background}&a={accent}&d={design}&t={Timestamp(font, fontMono, background, accent, design)}");
    }

    private bool ThemeFromQuery(string query, out string font, out string? fontMono, out string background, out string accent, out string design)
    {
        bool result = true;

        font = DefaultFont;
        background = DefaultBackground;
        accent = DefaultAccent;
        design = DefaultDesign;

        if (query.StartsWith('?'))
            foreach (string kv in query[1..].Split('&'))
                if (kv.SplitAtFirst('=', out string key, out string value))
                    switch (key)
                    {
                        case "f":
                            if (Fonts.Contains(value))
                                font = value;
                            else result = false;
                            break;
                        case "b":
                            if (Backgrounds.Contains(value))
                                background = value;
                            else result = false;
                            break;
                        case "a":
                            if (Accents.Contains(value))
                                accent = value;
                            else result = false;
                            break;
                        case "d":
                            if (Designs.Contains(value))
                                design = value;
                            else result = false;
                            break;
                    }

        fontMono = font switch
        {
            "ubuntu-mono" or "roboto-mono" or "special-elite" => null,
            "roboto" => "roboto-mono",
            "ubuntu" or _ => "ubuntu-mono"
        };

        return result;
    }

    private string Timestamp(string font, string? fontMono, string background, string accent, string design)
        => ((string[])
        [
            "/theme/base.css",
            $"/theme/f/{font}.css",
            ..(fontMono == null ? (string[])[] : ([$"/theme/f/{fontMono}.css"])),
            $"/theme/b/{background}.css",
            $"/theme/a/{AccentName(accent, background)}.css",
            $"/theme/d/{design}.css"
        ]).Max(x => long.Parse(GetFileVersion(x) ?? "0")).ToString();

    private static string AccentName(string accent, string background)
        => $"{accent}-{background switch
        {
            "light" or "white" => "light",
            "black" or "dark" or "beige" or _ => "dark"
        }}";
}