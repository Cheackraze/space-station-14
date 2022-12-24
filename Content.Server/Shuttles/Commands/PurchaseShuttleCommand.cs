using Content.Server.Administration;
using Content.Server.Shuttles.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Shuttles.Commands;

/// <summary>
/// purchases a shuttle.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class PurchaseShuttleCommand : IConsoleCommand
{
    public string Command => "purchaseshuttle";
    public string Description => "spawns and docks a specified shuttle from a grid file";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!int.TryParse(args[0], out var stationId))
        {
            shell.WriteError($"{args[0]} is not a valid integer.");
            return;
        }

        var shuttlePath = args[1];
        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ShuttleSystem>();
        var station = new EntityUid(stationId);
        system.PurchaseShuttle(station, shuttlePath);
    }
}
