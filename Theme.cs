using Org.BouncyCastle.Asn1.Ocsp;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    private readonly HashSet<string> Fonts = ["ubuntu", "ubuntu-mono"];
    private readonly HashSet<string> Backgrounds = ["black", "dark", "light", "white"];
    private readonly HashSet<string> Accents = ["green", "violet", "blue", "red"];
    private readonly HashSet<string> Designs = ["shadows", "layers", "flat", "no-css"];

    public string DefaultFont = "ubuntu";
    public string DefaultBackground = "dark";
    public string DefaultAccent = "blue";
    public string DefaultDesign = "layers";

    private static string AccentName(string accent, string background)
        => $"{accent}-{background switch
        {
            "light" or "white" => "light",
            "black" or "dark" or _ => "dark"
        }}";
}