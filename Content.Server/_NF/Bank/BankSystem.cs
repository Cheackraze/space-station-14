using System;
using Content.Server.GameTicking;
using Content.Shared.Bank;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Content.Shared.Database;
using Content.Server.Players;
using Content.Shared.Preferences;
using Content.Server.Database;
using Content.Server.Preferences.Managers;

namespace Content.Server._NF.Bank;

public sealed class BankSystem : EntitySystem
{
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetach);

    }
    // we may have to change this later depending on mind rework. then again, maybe the bank account should stay attached to the mob and not follow a mind around.
    private void OnPlayerSpawn (PlayerSpawnCompleteEvent args)
    {
        var mobUid = args.Mob;
        var bank = EnsureComp<BankAccountComponent>(mobUid);
        bank.Balance = args.Profile.BankBalance;
        Dirty(bank);

    }

    private void OnPlayerDetach (PlayerDetachedEvent args)
    {
        var mobUid = args.Entity;

        if (!TryComp<BankAccountComponent>(mobUid, out var bank))
            return;

        var user = args.Player.UserId;
        var prefs = _prefsManager.GetPreferences(user);
        var character = prefs.SelectedCharacter;
        var index = prefs.IndexOfCharacter(character);

        if (character is not HumanoidCharacterProfile profile)
            return;
        var newProfile = new HumanoidCharacterProfile(
            profile.Name,
            profile.FlavorText,
            profile.Species,
            profile.Age,
            profile.Sex,
            profile.Gender,
            bank.Balance,
            profile.Appearance,
            profile.Clothing,
            profile.Backpack,
            profile.JobPriorities,
            profile.PreferenceUnavailable,
            profile.AntagPreferences,
            profile.TraitPreferences);

        _dbManager.SaveCharacterSlotAsync(user, newProfile, index);
    }
}
