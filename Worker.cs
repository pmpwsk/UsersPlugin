namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin
{
    public override Task Work()
    {
        foreach (var table in Server.Config.Accounts.UserTables.Values.Distinct())
            foreach (var user in table.ListAll())
                if (user.Settings.TryGetValue("Delete", out var ticksString)
                    && long.TryParse(ticksString, out var ticks)
                    && (new DateTime(ticks) + TimeSpan.FromDays(30)) < DateTime.UtcNow)
                    table.Delete(user.Id);
        return Task.CompletedTask;
    }
}