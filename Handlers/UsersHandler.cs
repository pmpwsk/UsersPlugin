using uwap.WebFramework.Elements;
using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    private static async Task<IResponse> HandleUsers(Request req)
    {
        switch (req.Path)
        {
            // MAIN USER MANAGEMENT PAGE
            case "/users":
            { req.ForceGET();
                Presets.CreatePage(req, "Users", out var page, out var e);
                page.Navigation.Add(new Button("Back", ".", "right"));
                req.ForceAdmin();
                e.Add(new HeadingElement("Manage users"));
                foreach (var user in await req.UserTable.ListAllAsync())
                    e.Add(new ButtonElement(user.Username, user.MailAddress, $"users/user?id={user.Id}"));
                
                return new LegacyPageResponse(page, req);
            }




            // MANAGE USER
            case "/users/user":
            { req.ForceGET();
                Presets.CreatePage(req, "Users", out var page, out var e);
                page.Navigation.Add(new Button("Back", "../users", "right"));
                req.ForceAdmin();
                var id = req.Query.GetOrThrow("id");
                var user = await req.UserTable.GetByIdNullableAsync(id);
                if (user == null)
                    return StatusResponse.NotFound;
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
                
                return new LegacyPageResponse(page, req);
            }
            
            case "/users/user/settings/set":
            { req.ForcePOST(); req.ForceAdmin(false);
                var id = req.Query.GetOrThrow("id");
                var key = req.Query.GetOrThrow("key");
                var value = req.Query.GetOrThrow("value");
                await req.UserTable.SetSettingAsync(id, key, value);
                return StatusResponse.Success;
            }
            
            case "/users/user/settings/delete":
            { req.ForcePOST(); req.ForceAdmin(false);
                var id = req.Query.GetOrThrow("id");
                var key = req.Query.GetOrThrow("key");
                await req.UserTable.DeleteSettingAsync(id, key);
                return StatusResponse.Success;
            }

            case "/users/user/delete":
            { req.ForcePOST(); req.ForceAdmin(false);
                var id = req.Query.GetOrThrow("id");
                await req.UserTable.DeleteAsync(id);
                return StatusResponse.Success;
            }

            case "/users/user/set-access-level":
            { req.ForcePOST(); req.ForceAdmin(false);
                var id = req.Query.GetOrThrow("id");
                var value = req.Query.GetOrThrow<ushort>("value");
                await req.UserTable.SetAccessLevelAsync(id, value);
                return StatusResponse.Success;
            }




            // 404
            default:
                return StatusResponse.NotFound;
        }
    }
}