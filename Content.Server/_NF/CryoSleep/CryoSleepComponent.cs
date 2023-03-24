using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Medical.Cryopod;
[RegisterComponent]
public sealed class CryoSleepComponent : Component
{
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// The sound that is played when a player leaves the game via cryo
    /// </summary>
    [DataField("leaveSound")]
    public SoundSpecifier LeaveSound = new SoundPathSpecifier("/Audio/Effects/radpulse1.ogg");

}
