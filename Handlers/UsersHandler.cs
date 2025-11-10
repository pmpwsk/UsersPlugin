using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static Task HandleUsers(Request req)
    {
        switch (req.Path)
        {
            // MAIN USER MANAGEMENT PAGE
            case "/users":
            { req.ForceGET();
                req.CreatePage("Users", out var page, out var e);
                page.Navigation.Add(new Button("Back", ".", "right"));
                req.ForceAdmin();
                e.Add(new HeadingElement("Manage users"));
                foreach (var user in req.UserTable.ListAll())
                    e.Add(new ButtonElement(user.Username, user.MailAddress, $"users/user?id={user.Id}"));
            } break;




            // MANAGE USER
            case "/users/user":
            { req.ForceGET();
                req.CreatePage("Users", out var page, out var e);
                page.Navigation.Add(new Button("Back", "../users", "right"));
                req.ForceAdmin();
                if (!req.Query.TryGetValue("id", out var id))
                    throw new BadRequestSignal();
                if (!req.UserTable.TryGetValue(id, out var user))
                    throw new NotFoundSignal();
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("user.js"));
                e.Add(new HeadingElement(user.Username, new BulletList(
                    user.MailToken == null ? "Set up" : "Not set up",
                    user.MailAddress,
                    user.TwoFactor.TOTPEnabled() ? "2FA enabled" : "2FA disabled",
                    user.Signup.ToShortDateString())));
                if (user.Id != req.User.Id)
                {
                    e.Add(new ButtonElementJS(null, "Delete forever", $"DeleteUser('{user.Id}')", null, "red", "delete"));
                    e.Add(new ContainerElement("Access level", new TextBox("Enter a number (1-65535)...", user.AccessLevel.ToString(), "access-level", onEnter: $"SaveAccessLevel('{id}')", onInput: "AccessLevelChanged()"))
                    { Buttons = [new ButtonJS("Saved!", $"SaveAccessLevel('{id}')", id: "save"), new ButtonJS("Normal", $"SetAccessLevel('{id}','1')"), new ButtonJS("Admin", $"SetAccessLevel('{id}','65535')")] });
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
                    e.Add(new ContainerElement(key, user.Settings.Get(key)) { Button = new ButtonJS("Delete", $"DeleteSetting('{id}', '{key}')", "red") });
            } break;
            
            case "/users/user/settings/set":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (req.Query.TryGetValue("id", out var id) && req.Query.TryGetValue("key", out var key) && req.Query.TryGetValue("value", out var value))
                    req.UserTable.SetSetting(id, key, value);
                else req.Status = 400;
            } break;
            
            case "/users/user/settings/delete":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (req.Query.TryGetValue("id", out var id) && req.Query.TryGetValue("key", out var key))
                    req.UserTable.DeleteSetting(id, key);
                else req.Status = 400;
            } break;

            case "/users/user/delete":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (req.Query.TryGetValue("id", out var id))
                    req.UserTable.Delete(id);
                else req.Status = 400;
            } break;

            case "/users/user/set-access-level":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (req.Query.TryGetValue("id", out string? id) && req.Query.TryGetValue("value", out ushort value))
                    req.UserTable.SetAccessLevel(id, value);
                else req.Status = 400;
            } break;




            // 404
            default:
                req.CreatePage("Error");
                req.Status = 404;
                break;
        }

        return Task.CompletedTask;
    }
}