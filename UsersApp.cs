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
                {
                    page.Title = "Users";
                    if (request.Query.TryGetValue("id", out var id))
                    {
                        if (!request.UserTable.TryGetValue(id, out var user))
                        {
                            request.Status = 404;
                            break;
                        }
                        page.Scripts.Add(new Script($"{pathPrefix}/users-view.js"));
                        e.Add(new HeadingElement(user.Username, new BulletList(
                            user.MailToken == null ? "Set up" : "Not set up",
                            user.MailAddress,
                            user.TwoFactor.TOTPEnabled() ? "2FA enabled" : "2FA disabled",
                            user.Signup.ToShortDateString())));
                        if (user.Id != request.User.Id)
                        {
                            e.Add(new ButtonElementJS(null, "Delete forever", $"DeleteUser('{user.Id}')", null, "red", "delete"));
                            e.Add(new ContainerElement("Access level", new TextBox("Enter a number (1-65535)...", user.AccessLevel.ToString(), "access-level", onEnter: $"SaveAccessLevel('{id}')", onInput: "AccessLevelChanged()"))
                            { Buttons = [new ButtonJS("Saved!", $"SaveAccessLevel('{id}')", id: "save"), new ButtonJS("Normal", $"SetAccessLevel('{id}','1')"), new ButtonJS("Admin", $"SetAccessLevel('{id}','65355')")] });
                        }
                        page.AddError();
                        e.Add(new ContainerElement(null,
                        [
                            new Heading("Key"),
                            new TextBox("Enter a key...", null, "key", TextBoxRole.NoSpellcheck, $"Set('{id}')"),
                            new Heading("Value"),
                            new TextBox("Enter a value...", null, "value", TextBoxRole.NoSpellcheck, $"Set('{id}')")
                        ])
                        { Button = new ButtonJS("Set value", $"SetSetting('{id}')", "green") });
                        foreach (var key in user.Settings.ListKeys())
                            e.Add(new ContainerElement(key, user.Settings[key]) { Button = new ButtonJS("Delete", $"DeleteSetting('{id}', '{key}')", "red") });
                    }
                    else
                    {
                        foreach (var kv in request.UserTable)
                            e.Add(new ButtonElement(kv.Value.Username, kv.Value.MailAddress, $"{pathPrefix}/users?id={kv.Value.Id}"));
                    }
                }
                break;
            default:
                request.Status = 404;
                break;
        }
    }
}