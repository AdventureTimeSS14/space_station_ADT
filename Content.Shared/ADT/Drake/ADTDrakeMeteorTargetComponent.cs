using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Drake;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTDrakeMeteorTargetComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.9);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ImpactAt;

    [DataField]
    public float Damage = 10f;

    [DataField]
    public EntProtoId FireProto = "ADTDrakeFire";

    [DataField]
    public EntProtoId FallingProto = "ADTDrakeFallingFireball";

    [DataField]
    public SoundSpecifier SpawnSound = new SoundPathSpecifier("/Audio/ADT/Drake/fleshtostone.ogg");

    [DataField]
    public SoundSpecifier ImpactSound = new SoundCollectionSpecifier("Explosion");
}
