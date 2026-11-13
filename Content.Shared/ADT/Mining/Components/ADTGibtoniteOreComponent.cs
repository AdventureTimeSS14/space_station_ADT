using Content.Shared.ADT.Explosion;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Mining.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class ADTGibtoniteOreComponent : Component
{
    [DataField]
    public ADTGibtoniteQuality Quality = ADTGibtoniteQuality.Low;

    [DataField]
    public bool Primed;

    [DataField]
    public TimeSpan DetonationDelay = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan DetonateAt;

    [DataField]
    public float MinForce = 10f;

    [DataField]
    public SoundSpecifier PrimeSound = new SoundPathSpecifier("/Audio/ADT/Mining/hit_on_shattered_glass.ogg");

    [DataField]
    public EntProtoId Shard = "ADTGibtoniteShard";

    [DataField]
    public int ShardCount = 3;

    [DataField]
    public ADTBlastRadii LowExplosion = new(-1, 1, 3);

    [DataField]
    public ADTBlastRadii MediumExplosion = new(1, 2, 5);

    [DataField]
    public ADTBlastRadii HighExplosion = new(2, 4, 9);
}

[RegisterComponent]
public sealed partial class ADTPrimedGibtoniteComponent : Component;
