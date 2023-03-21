using Content.Shared.Procedural;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    /// <summary>
    /// Generates a dungeon similar to the command, but in space and with a public api to hoook into a game rule system.
    /// </summary>
    public async void GenerateDungeon(MapId mapId, Vector2 targetPos, DungeonConfigPrototype dungeon, int seed)
    {
    // Multithread gods please spare me.
        var dungeonUid = _mapManager.GetMapEntityId(mapId);

        if (!TryComp<MapGridComponent>(dungeonUid, out var dungeonGrid))
        {
            dungeonUid = EntityManager.CreateEntityUninitialized(null, new EntityCoordinates(dungeonUid, targetPos));
            dungeonGrid = EntityManager.AddComponent<MapGridComponent>(dungeonUid);
            EntityManager.InitializeAndStartEntity(dungeonUid, mapId);
        }

        GenerateDungeon(dungeon, dungeonUid, dungeonGrid, targetPos, seed);
    }
}
