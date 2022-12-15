using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// A component for marking spaces to be spawnpoints for FFA
/// </summary>
[RegisterComponent]
public sealed class FreeForAllSpawnComponent : Component
{
    public GameTick LastSpawn = GameTick.Zero;
}
