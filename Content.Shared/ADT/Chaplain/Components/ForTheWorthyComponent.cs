using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Chaplain.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class ForTheWorthyComponent : Component
{
    [DataField]
    public SoundSpecifier ElectrocutionSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    [DataField]
    public float Damage = 5f;

    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan PunishDelay = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPunish;
}
