using Org.BouncyCastle.Asn1.Ocsp;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    private readonly HashSet<string> Fonts = ["ubuntu", "ubuntu-mono"];
    private readonly HashSet<string> Backgrounds = ["black", "dark", "light", "white"];
    private readonly HashSet<string> Accents = ["green", "violet", "blue", "red"];
    private readonly HashSet<string> Designs = ["shadows", "layers", "flat", "no-css"];
}