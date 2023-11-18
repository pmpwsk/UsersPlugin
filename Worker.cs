namespace uwap.WebFramework.Plugins;

public partial class UsersPlugin : Plugin
{
    public override Task Work()
    {
        foreach (var table in Server.Config.Accounts.UserTables.Values.Distinct())
            foreach (var kv in table)
                if (kv.Value.Settings.TryGetValue("Delete", out var ticksString)
                    && long.TryParse(ticksString, out var ticks)
                    && (new DateTime(ticks) + TimeSpan.FromDays(30)) < DateTime.UtcNow)
                    table.Delete(kv.Key);
        return Task.CompletedTask;
    }
}