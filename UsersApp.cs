using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public static void Users(AppRequest request, string path, string pathPrefix)
    {
        if (!request.IsAdmin())
        {
            request.Status = 403;
            return;
        }
        Presets.Init(request, out Page page, out var e);
        switch(path)
        {
            case "":
                page.Title = "Users";
                var userTable = request.UserTable;
                if (request.Query.ContainsKey("id"))
                {
                    string id = request.Query["id"];
                    if (!userTable.ContainsKey(id))
                    {
                        request.Status = 404;
                        break;
                    }
                    page.Scripts.Add(new Script($"{pathPrefix}/users-view.js"));
                    User user = userTable[id];
                    e.Add(new HeadingElement(user.Username, new BulletList(
                        user.MailToken == null ? "Set up" : "Not set up",
                        user.MailAddress,
                        user.TwoFactor.TOTPEnabled() ? "2FA enabled" : "2FA disabled",
                        user.Signup.ToShortDateString())));
                    if (request.User != null && user.Id != request.User.Id)
                    {
                        e.Add(new ButtonElementJS(null, "Delete forever", $"DeleteUser('{user.Id}')", null, "red", "delete"));
                    }
                    e.Add(new ContainerElement(null, new List<IContent>
                    {
                        new Heading("Key"),
                        new TextBox("Enter a key...", null, "key", TextBoxRole.NoSpellcheck, $"Set('{id}')"),
                        new Heading("Value"),
                        new TextBox("Enter a value...", null, "value", TextBoxRole.NoSpellcheck, $"Set('{id}')")
                    })
                    { Button = new ButtonJS("Set value", $"SetSetting('{id}')", "green") });
                    page.AddError();
                    foreach (var key in user.Settings.ListKeys())
                        e.Add(new ContainerElement(key, user.Settings[key]) { Button = new ButtonJS("Delete", $"DeleteSetting('{id}', '{key}')", "red") });
                }
                else
                {
                    var users = userTable.ListKeys().Select(x => userTable[x]);
                    foreach (User user in users)
                    {
                        e.Add(new ButtonElement(user.Username, user.MailAddress, $"{pathPrefix}/users?id={user.Id}"));
                    }
                }
                break;
            default:
                request.Status = 404;
                break;
        }
    }
}