using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Configurations;

public sealed class FreeForAllRuleConfiguration : GameRuleConfiguration
{
    public override string Id => "FreeForAll";

    [DataField("killsToWin")] public readonly int KillsToWin = 1;//30;

    [DataField("respawnTime")] public readonly float RespawnTime = 3f;

    [DataField("randomHumanoidSettings",
        customTypeSerializer: typeof(PrototypeIdSerializer<RandomHumanoidSettingsPrototype>))]
    public readonly string RandomHumanoidSettingsPrototype = "NukeOp";

    [DataField("gear",
        customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public readonly string StartingGearPrototype = "ERTLeaderGearEVA";

}
