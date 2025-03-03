namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
	public override byte[]? GetFile(string relPath, string pathPrefix, string domain)
		=> relPath switch
		{
			"/2fa.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File0"),
			"/auth-request.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File1"),
			"/login.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File2"),
			"/register.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File3"),
			"/verify.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File4"),
			"/fonts/roboto-mono.eot" => (byte[]?)PluginFiles_ResourceManager.GetObject("File5"),
			"/fonts/roboto-mono.otf" => (byte[]?)PluginFiles_ResourceManager.GetObject("File6"),
			"/fonts/roboto-mono.svg" => (byte[]?)PluginFiles_ResourceManager.GetObject("File7"),
			"/fonts/roboto-mono.ttf" => (byte[]?)PluginFiles_ResourceManager.GetObject("File8"),
			"/fonts/roboto-mono.woff" => (byte[]?)PluginFiles_ResourceManager.GetObject("File9"),
			"/fonts/roboto-mono.woff2" => (byte[]?)PluginFiles_ResourceManager.GetObject("File10"),
			"/fonts/roboto.eot" => (byte[]?)PluginFiles_ResourceManager.GetObject("File11"),
			"/fonts/roboto.otf" => (byte[]?)PluginFiles_ResourceManager.GetObject("File12"),
			"/fonts/roboto.svg" => (byte[]?)PluginFiles_ResourceManager.GetObject("File13"),
			"/fonts/roboto.ttf" => (byte[]?)PluginFiles_ResourceManager.GetObject("File14"),
			"/fonts/roboto.woff" => (byte[]?)PluginFiles_ResourceManager.GetObject("File15"),
			"/fonts/roboto.woff2" => (byte[]?)PluginFiles_ResourceManager.GetObject("File16"),
			"/fonts/special-elite.eot" => (byte[]?)PluginFiles_ResourceManager.GetObject("File17"),
			"/fonts/special-elite.otf" => (byte[]?)PluginFiles_ResourceManager.GetObject("File18"),
			"/fonts/special-elite.svg" => (byte[]?)PluginFiles_ResourceManager.GetObject("File19"),
			"/fonts/special-elite.ttf" => (byte[]?)PluginFiles_ResourceManager.GetObject("File20"),
			"/fonts/special-elite.woff" => (byte[]?)PluginFiles_ResourceManager.GetObject("File21"),
			"/fonts/special-elite.woff2" => (byte[]?)PluginFiles_ResourceManager.GetObject("File22"),
			"/fonts/ubuntu-mono.eot" => (byte[]?)PluginFiles_ResourceManager.GetObject("File23"),
			"/fonts/ubuntu-mono.otf" => (byte[]?)PluginFiles_ResourceManager.GetObject("File24"),
			"/fonts/ubuntu-mono.svg" => (byte[]?)PluginFiles_ResourceManager.GetObject("File25"),
			"/fonts/ubuntu-mono.ttf" => (byte[]?)PluginFiles_ResourceManager.GetObject("File26"),
			"/fonts/ubuntu-mono.woff" => (byte[]?)PluginFiles_ResourceManager.GetObject("File27"),
			"/fonts/ubuntu-mono.woff2" => (byte[]?)PluginFiles_ResourceManager.GetObject("File28"),
			"/fonts/ubuntu.eot" => (byte[]?)PluginFiles_ResourceManager.GetObject("File29"),
			"/fonts/ubuntu.otf" => (byte[]?)PluginFiles_ResourceManager.GetObject("File30"),
			"/fonts/ubuntu.svg" => (byte[]?)PluginFiles_ResourceManager.GetObject("File31"),
			"/fonts/ubuntu.ttf" => (byte[]?)PluginFiles_ResourceManager.GetObject("File32"),
			"/fonts/ubuntu.woff" => (byte[]?)PluginFiles_ResourceManager.GetObject("File33"),
			"/fonts/ubuntu.woff2" => (byte[]?)PluginFiles_ResourceManager.GetObject("File34"),
			"/recovery/username.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File35"),
			"/recovery/password/request.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File36"),
			"/recovery/password/set.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File37"),
			"/settings/2fa.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File38"),
			"/settings/apps.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File39"),
			"/settings/delete.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File40"),
			"/settings/email-verify.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File41"),
			"/settings/email.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File42"),
			"/settings/password.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File43"),
			"/settings/theme.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File44"),
			"/settings/username.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File45"),
			"/theme/base.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File46"),
			"/theme/a/blue-dark.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File47"),
			"/theme/a/blue-light.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File48"),
			"/theme/a/green-dark.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File49"),
			"/theme/a/green-light.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File50"),
			"/theme/a/none-dark.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File51"),
			"/theme/a/none-light.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File52"),
			"/theme/a/red-dark.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File53"),
			"/theme/a/red-light.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File54"),
			"/theme/a/violet-dark.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File55"),
			"/theme/a/violet-light.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File56"),
			"/theme/b/beige.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File57"),
			"/theme/b/black.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File58"),
			"/theme/b/dark.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File59"),
			"/theme/b/light.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File60"),
			"/theme/b/white.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File61"),
			"/theme/d/flat.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File62"),
			"/theme/d/layers.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File63"),
			"/theme/d/shadows.css" => (byte[]?)PluginFiles_ResourceManager.GetObject("File64"),
			"/theme/f/roboto-mono.css" => System.Text.Encoding.UTF8.GetBytes($"@font-face {{\n    font-family: 'roboto-mono';\n    font-style: normal;\n    font-weight: 400;\n    font-display: block;\n    src: url('{pathPrefix}/fonts/roboto-mono.woff2') format('woff2'), url('{pathPrefix}/fonts/roboto-mono.woff') format('woff'), url('{pathPrefix}/fonts/roboto-mono.ttf') format('truetype'), url('{pathPrefix}/fonts/roboto-mono.otf') format('otf'), url('{pathPrefix}/fonts/roboto-mono.eot') format('embedded-opentype'), url('{pathPrefix}/fonts/roboto-mono.svg') format('svg');\n    unicode-range: U+000-5FF;\n    size-adjust: 85%;\n}}\n"),
			"/theme/f/roboto.css" => System.Text.Encoding.UTF8.GetBytes($"@font-face {{\n    font-family: 'roboto';\n    font-style: normal;\n    font-weight: 400;\n    font-display: block;\n    src: url('{pathPrefix}/fonts/roboto.woff2') format('woff2'), url('{pathPrefix}/fonts/roboto.woff') format('woff'), url('{pathPrefix}/fonts/roboto.ttf') format('truetype'), url('{pathPrefix}/fonts/roboto.otf') format('otf'), url('{pathPrefix}/fonts/roboto.eot') format('embedded-opentype'), url('{pathPrefix}/fonts/roboto.svg') format('svg');\n    unicode-range: U+000-5FF;\n    size-adjust: 95%;\n}}\n"),
			"/theme/f/special-elite.css" => System.Text.Encoding.UTF8.GetBytes($"@font-face {{\n    font-family: 'special-elite';\n    font-style: normal;\n    font-weight: 400;\n    font-display: block;\n    src: url('{pathPrefix}/fonts/special-elite.woff2') format('woff2'), url('{pathPrefix}/fonts/special-elite.woff') format('woff'), url('{pathPrefix}/fonts/special-elite.ttf') format('truetype'), url('{pathPrefix}/fonts/special-elite.otf') format('otf'), url('{pathPrefix}/fonts/special-elite.eot') format('embedded-opentype'), url('{pathPrefix}/fonts/special-elite.svg') format('svg');\n    unicode-range: U+000-5FF;\n    size-adjust: 102%;\n}}\n"),
			"/theme/f/ubuntu-mono.css" => System.Text.Encoding.UTF8.GetBytes($"@font-face {{\n    font-family: 'ubuntu-mono';\n    font-style: normal;\n    font-weight: 400;\n    font-display: block;\n    src: url('{pathPrefix}/fonts/ubuntu-mono.woff2') format('woff2'), url('{pathPrefix}/fonts/ubuntu-mono.woff') format('woff'), url('{pathPrefix}/fonts/ubuntu-mono.ttf') format('truetype'), url('{pathPrefix}/fonts/ubuntu-mono.otf') format('otf'), url('{pathPrefix}/fonts/ubuntu-mono.eot') format('embedded-opentype'), url('{pathPrefix}/fonts/ubuntu-mono.svg') format('svg');\n    unicode-range: U+000-5FF;\n    size-adjust: 101%;\n}}\n"),
			"/theme/f/ubuntu.css" => System.Text.Encoding.UTF8.GetBytes($"@font-face {{\n    font-family: 'ubuntu';\n    font-style: normal;\n    font-weight: 400;\n    font-display: block;\n    src: url('{pathPrefix}/fonts/ubuntu.woff2') format('woff2'), url('{pathPrefix}/fonts/ubuntu.woff') format('woff'), url('{pathPrefix}/fonts/ubuntu.ttf') format('truetype'), url('{pathPrefix}/fonts/ubuntu.otf') format('otf'), url('{pathPrefix}/fonts/ubuntu.eot') format('embedded-opentype'), url('{pathPrefix}/fonts/ubuntu.svg') format('svg');\n    unicode-range: U+000-5FF;\n}}\n"),
			"/users/user.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File65"),
			"/verify/change.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File66"),
			_ => null
		};
	
	public override string? GetFileVersion(string relPath)
		=> relPath switch
		{
			"/2fa.js" => "1719280992497",
			"/auth-request.js" => "1722551602845",
			"/login.js" => "1719279117889",
			"/register.js" => "1719436547597",
			"/verify.js" => "1719436553974",
			"/fonts/roboto-mono.eot" => "1705550294941",
			"/fonts/roboto-mono.otf" => "1705550294958",
			"/fonts/roboto-mono.svg" => "1705550294979",
			"/fonts/roboto-mono.ttf" => "1705550294994",
			"/fonts/roboto-mono.woff" => "1705550295009",
			"/fonts/roboto-mono.woff2" => "1705550295023",
			"/fonts/roboto.eot" => "1705550149578",
			"/fonts/roboto.otf" => "1705550149594",
			"/fonts/roboto.svg" => "1705550149619",
			"/fonts/roboto.ttf" => "1705550149635",
			"/fonts/roboto.woff" => "1705550149650",
			"/fonts/roboto.woff2" => "1705550149664",
			"/fonts/special-elite.eot" => "1707582737000",
			"/fonts/special-elite.otf" => "1707582737000",
			"/fonts/special-elite.svg" => "1707582737000",
			"/fonts/special-elite.ttf" => "1707582737000",
			"/fonts/special-elite.woff" => "1707582737000",
			"/fonts/special-elite.woff2" => "1707582737000",
			"/fonts/ubuntu-mono.eot" => "1704037913000",
			"/fonts/ubuntu-mono.otf" => "1704037913000",
			"/fonts/ubuntu-mono.svg" => "1704037913000",
			"/fonts/ubuntu-mono.ttf" => "1704037913000",
			"/fonts/ubuntu-mono.woff" => "1704037913000",
			"/fonts/ubuntu-mono.woff2" => "1704037913000",
			"/fonts/ubuntu.eot" => "1704037913000",
			"/fonts/ubuntu.otf" => "1704037913000",
			"/fonts/ubuntu.svg" => "1704037913000",
			"/fonts/ubuntu.ttf" => "1704037913000",
			"/fonts/ubuntu.woff" => "1704037913000",
			"/fonts/ubuntu.woff2" => "1704037913000",
			"/recovery/username.js" => "1719478520571",
			"/recovery/password/request.js" => "1719479031806",
			"/recovery/password/set.js" => "1719494426437",
			"/settings/2fa.js" => "1719435530856",
			"/settings/apps.js" => "1719443216389",
			"/settings/delete.js" => "1719439596725",
			"/settings/email-verify.js" => "1719439033826",
			"/settings/email.js" => "1719438677308",
			"/settings/password.js" => "1719446553660",
			"/settings/theme.js" => "1719435551060",
			"/settings/username.js" => "1719438394007",
			"/theme/base.css" => "1736878385960",
			"/theme/a/blue-dark.css" => "1705337749422",
			"/theme/a/blue-light.css" => "1705335547954",
			"/theme/a/green-dark.css" => "1705335236932",
			"/theme/a/green-light.css" => "1705337834244",
			"/theme/a/none-dark.css" => "1707515935153",
			"/theme/a/none-light.css" => "1707515935153",
			"/theme/a/red-dark.css" => "1705338739319",
			"/theme/a/red-light.css" => "1705338156853",
			"/theme/a/violet-dark.css" => "1705335294977",
			"/theme/a/violet-light.css" => "1705335705662",
			"/theme/b/beige.css" => "1707511909060",
			"/theme/b/black.css" => "1705351277204",
			"/theme/b/dark.css" => "1705352678157",
			"/theme/b/light.css" => "1705353000732",
			"/theme/b/white.css" => "1705353054167",
			"/theme/d/flat.css" => "1736806133631",
			"/theme/d/layers.css" => "1736806127850",
			"/theme/d/shadows.css" => "1736806137821",
			"/theme/f/roboto-mono.css" => "1719245973198",
			"/theme/f/roboto.css" => "1719245977465",
			"/theme/f/special-elite.css" => "1719245979775",
			"/theme/f/ubuntu-mono.css" => "1719245982355",
			"/theme/f/ubuntu.css" => "1719245991215",
			"/users/user.js" => "1719448365162",
			"/verify/change.js" => "1719436525727",
			_ => null
		};
	
	private static readonly System.Resources.ResourceManager PluginFiles_ResourceManager = new("UsersPlugin.Properties.PluginFiles", typeof(UsersPlugin).Assembly);
}