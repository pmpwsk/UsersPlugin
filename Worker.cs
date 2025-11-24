namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    public override async Task Work()
    {
        foreach (var table in Server.Config.Accounts.UserTables.Values.Distinct())
            foreach (var user in await table.ListAllAsync())
                if (user.Settings.TryGetValue("Delete", out var ticksString)
                    && long.TryParse(ticksString, out var ticks)
                    && (new DateTime(ticks) + TimeSpan.FromDays(30)) < DateTime.UtcNow)
                    await table.DeleteAsync(user.Id);
    }
}