using Content.Server.Procedural;
using Content.Shared.Bank.Components;
using Content.Server.GameTicking.Events;
using Content.Shared.Procedural;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Map.Components;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// This handles the dungeon and trading post spawning, as well as round end capitalism summary
/// </summary>
public sealed class NfAdventureRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly DungeonSystem _dunGen = default!;
    [Dependency] private readonly IConsoleHost _console = default!;

    [ViewVariables]
    private List<(EntityUid, int)> _players = new();
    [ViewVariables]
    private MapId _currentWorld = new();
    [ViewVariables]
    private int _totalBalance = 0;

    public override string Prototype => "Adventure";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnStartup);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawningEvent);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextEvent);
    }

    private void OnRoundEndTextEvent(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        ev.AddLine(Loc.GetString("adventure-list-start"));
        foreach (var player in _players)
        {
            if (!TryComp<BankAccountComponent>(player.Item1, out var bank) || !TryComp<MetaDataComponent>(player.Item1, out var meta))
                continue;

            var profit = bank.Balance - player.Item2;
            ev.AddLine($"- {meta.EntityName} adventure-mode-profit-text { profit } currency");
        }
    }

    public override void Started() { }

    public override void Ended() { }

    private void OnPlayerSpawningEvent(PlayerSpawnCompleteEvent ev)
    {
        if (!RuleAdded)
        {
            return;
        }
        if (ev.Player.AttachedEntity is { Valid : true } mobUid)
        {
            _players.Add((mobUid, ev.Profile.BankBalance));
        }

    }

    private void OnStartup(RoundStartingEvent ev)
    {
        if (!RuleAdded)
            return;

        var depotMap = "/Maps/cargodepot.yml";
        var mapId = GameTicker.DefaultMap;
        if (_map.TryLoad(mapId, depotMap, out var depotUids, new MapLoadOptions
        {
            Offset = _random.NextVector2(1500f, 3500f)
        }))
        {
            var meta = EnsureComp<MetaDataComponent>(depotUids[0]);
            meta.EntityName = "NT Cargo Depot NF14";
        };

        var dungenTypes = _prototypeManager.EnumeratePrototypes<DungeonConfigPrototype>();

        foreach (var dunGen in dungenTypes)
        {

            var seed = _random.Next();
            var offset = _random.NextVector2(3500f, 6000f);
            if (!_map.TryLoad(mapId, "/Maps/spaceplatform.yml", out var grids, new MapLoadOptions
            {
                Offset = offset
            }))
            {
                continue;
            }

            var mapGrid = EnsureComp<MapGridComponent>(grids[0]);
            _console.WriteLine(null, $"dungeon spawned at {offset}");
            offset = new Vector2 (0, 0);

            //pls fit the grid I beg, this is so hacky
            _dunGen.GenerateDungeon(dunGen, grids[0], mapGrid, offset, seed);
        }
    }
}
