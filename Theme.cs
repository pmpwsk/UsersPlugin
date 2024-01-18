using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    private readonly HashSet<string> Fonts = ["ubuntu", "ubuntu-mono", "roboto", "roboto-mono"];
    private readonly HashSet<string> Backgrounds = ["black", "dark", "light", "white"];
    private readonly HashSet<string> Accents = ["green", "violet", "blue", "red"];
    private readonly HashSet<string> Designs = ["shadows", "layers", "flat", "no-css"];

    public string DefaultFont = "roboto";
    public string DefaultBackground = "dark";
    public string DefaultAccent = "blue";
    public string DefaultDesign = "layers";


    public Style GetStyle(IRequest request, out string fontUrl, string pathPrefix)
    {
        ThemeFromQuery((request.LoggedIn && request.User.Settings.TryGetValue("Theme", out string? theme)) ? theme : "default", out string font, out string? fontMono, out string background, out string accent, out string design);
        fontUrl = $"{pathPrefix}/fonts/{font}.woff2";
        return new Style($"/api{pathPrefix}/theme.css?f={font}&b={background}&a={accent}&d={design}&t={Timestamp(font, fontMono, background, accent, design)}");
    }

    private void ThemeFromQuery(string query, out string font, out string? fontMono, out string background, out string accent, out string design)
    {
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
                            break;
                        case "b":
                            if (Backgrounds.Contains(value))
                                background = value;
                            break;
                        case "a":
                            if (Accents.Contains(value))
                                accent = value;
                            break;
                        case "d":
                            if (Designs.Contains(value))
                                design = value;
                            break;
                    }

        fontMono = font switch
        {
            "ubuntu-mono" or "roboto-mono" => null,
            "roboto" => "roboto-mono",
            "ubuntu" or _ => "ubuntu-mono"
        };
    }

    private string Timestamp(string font, string? fontMono, string background, string accent, string design)
        => ((string[])
        [
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
            "black" or "dark" or _ => "dark"
        }}";
}