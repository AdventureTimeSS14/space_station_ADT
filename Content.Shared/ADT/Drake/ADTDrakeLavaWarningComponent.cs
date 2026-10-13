using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Drake;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTDrakeLavaWarningComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1.3);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ImpactAt;

    [DataField]
    public TimeSpan ResetTime = TimeSpan.FromSeconds(1);

    [DataField]
    public float Damage = 10f;

    [DataField]
    public float MechDamage = 45f;

    [DataField]
    public EntProtoId TempLavaProto = "ADTDrakeTempLava";

    [DataField]
    public List<EntProtoId> LavaPrototypes = new() { "FloorLavaEntity", "ADTDrakeTempLava" };

    [DataField]
    public SoundSpecifier SpawnSound = new SoundPathSpecifier("/Audio/ADT/Drake/fleshtostone.ogg");

    [DataField]
    public SoundSpecifier ImpactSound = new SoundPathSpecifier("/Audio/Magic/fireball.ogg");
}
