using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Telephone;

/// <summary>
/// Handheld telephone for the quartermaster and salvage specialists.
/// Works on top of the vanilla telephone system.
/// </summary>
[RegisterComponent]
public sealed partial class ADTPhoneComponent : Component
{
    [DataField]
    public bool DoNotDisturb;

    [DataField]
    public TimeSpan CallCooldown = TimeSpan.FromSeconds(1.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCall;

    [DataField]
    public SoundSpecifier? RingOutgoingSound = new SoundPathSpecifier("/Audio/ADT/Phone/ring_outgoing.ogg");

    [DataField]
    public SoundSpecifier? BusySound = new SoundPathSpecifier("/Audio/ADT/Phone/phone_busy.ogg");

    [DataField]
    public SoundSpecifier? PickupSound = new SoundPathSpecifier("/Audio/ADT/Phone/remote_pickup.ogg");

    [DataField]
    public SoundSpecifier? HangUpSound = new SoundPathSpecifier("/Audio/ADT/Phone/remote_hangup.ogg");
}
