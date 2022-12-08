using System.Linq;
using Content.Server.CharacterAppearance.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Humanoid.Systems;
using Content.Server.Players;
using Content.Server.Preferences.Managers;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.MobState;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed class FreeForAllRuleSystem : GameRuleSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly RandomHumanoidSystem _randomHumanoid = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly IPlayerManager _playerSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override string Prototype => "FreeForAll";

    private FreeForAllRuleConfiguration _freeForAllRuleConfiguration = default!;

    private readonly Dictionary<string, int> _kills = new();

    private readonly Dictionary<IPlayerSession, float> _spawnQueue = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnPlayerSpawning);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        _playerSystem.PlayerStatusChanged += OnStatusChanged;
    }

    private void OnStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if(e.NewStatus != SessionStatus.Disconnected)
            return;

        _spawnQueue.Remove(e.Session);
    }

    public override void Update(float frameTime)
    {
        if(_gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        foreach (var player in _spawnQueue.Keys)
        {
            _spawnQueue[player] -= frameTime;
            if (_spawnQueue[player] <= 0)
            {
                _spawnQueue.Remove(player);
                SpawnPlayer(player);
            }
        }
    }

    private void OnPlayerSpawning(PlayerBeforeSpawnEvent ev)
    {
        if (!RuleAdded || ev.Handled)
            return;

        SpawnPlayer(ev.Player);
        ev.Handled = true;
    }

    private void SpawnPlayer(IPlayerSession session)
    {
        if (!_kills.ContainsKey(session.Name))
            _kills[session.Name] = 0;
        var spawns = EntityQueryEnumerator<FreeForAllSpawnComponent, TransformComponent>();

        if(!spawns.MoveNext(out var spawnComponent, out var transformComponent))
        {
            Logger.Error("Could not find spawnpoint for spawning in FFA.");
            return;
        }

        var coords = transformComponent.Coordinates;
        var lastSpawnComp = spawnComponent;
        while (spawns.MoveNext(out spawnComponent, out transformComponent))
        {
            if (lastSpawnComp.LastSpawn > spawnComponent.LastSpawn)
            {
                coords = transformComponent.Coordinates;
                lastSpawnComp = spawnComponent;
            }
        }

        lastSpawnComp.LastSpawn = _gameTiming.CurTick;

        var mob = _randomHumanoid.SpawnRandomHumanoid(_freeForAllRuleConfiguration.RandomHumanoidSettingsPrototype,
            coords, "placeholder"); //todo paul
        var profile = _prefs.GetPreferences(session.UserId).SelectedCharacter as HumanoidCharacterProfile;

        EntityManager.EnsureComponent<RandomHumanoidAppearanceComponent>(mob);

        _stationSpawningSystem.EquipStartingGear(mob,
            _prototypeManager.Index<StartingGearPrototype>(_freeForAllRuleConfiguration.StartingGearPrototype),
            profile);

        var newMind = new Mind.Mind(session.UserId)
        {
            CharacterName = "placeholder"
        };
        newMind.ChangeOwningPlayer(session.UserId);
        newMind.TransferTo(mob);

        GameTicker.PlayerJoinGame(session);

        _chatManager.DispatchServerMessage(session, Loc.GetString("ffa-welcome"));
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        var players = _kills.Keys.ToList();
        if (players.Count == 0)
        {
            ev.AddLine("No players played this round of ffa :(");
            return;
        }

        if (players.Count == 1)
        {
            ev.AddLine($"{players[0]}, congratulations, you played yourself.");
            return;
        }

        players.Sort((a, b) => _kills[b].CompareTo(_kills[a]));
        ev.AddLine($"{players[0]} won!");
        ev.AddLine("============================");
        foreach (var player in players)
        {
            ev.AddLine($"{player} | {_kills[player]}");
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!RuleAdded || ev.Entity == ev.Origin || ev.CurrentMobState == DamageState.Alive || !TryComp<ActorComponent>(ev.Origin, out var actorComponent))
            return;

        if (!_kills.ContainsKey(actorComponent.PlayerSession.Name))
            _kills[actorComponent.PlayerSession.Name] = 0;

        _kills[actorComponent.PlayerSession.Name]++;

        _chatManager.DispatchServerMessage(actorComponent.PlayerSession, Loc.GetString("ffa-did-kill", ("name", Name(ev.Entity))));

        //ghost the other player
        if (TryComp<ActorComponent>(ev.Entity, out var victimActor))
        {
            if (victimActor.PlayerSession.ContentData()?.Mind is { } victimMind)
                _gameTicker.OnGhostAttempt(victimMind, false);

            _spawnQueue[victimActor.PlayerSession] = _freeForAllRuleConfiguration.RespawnTime;
            _chatManager.DispatchServerMessage(victimActor.PlayerSession, Loc.GetString("ffa-got-killed", ("name", Name(ev.Origin.Value))));
            RemComp<ActorComponent>(ev.Entity);
        }

        if (_kills[actorComponent.PlayerSession.Name] >= _freeForAllRuleConfiguration.KillsToWin)
        {
            _roundEndSystem.EndRound();
        }
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if(!RuleAdded)
            return;

        if (Configuration is not FreeForAllRuleConfiguration freeForAllRuleConfiguration)
        {
            Logger.Error($"{nameof(FreeForAllRuleSystem)} but something other than {nameof(FreeForAllRuleConfiguration)} was used: {Configuration.GetType()}");
            return;
        }

        _freeForAllRuleConfiguration = freeForAllRuleConfiguration;
    }

    public override void Started()
    {
        _kills.Clear();
    }

    public override void Ended()
    {
        _spawnQueue.Clear();
    }
}
