using Content.Shared.ADT.Explosion;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Mining.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class ADTGibtoniteDepositComponent : Component
{
    [DataField]
    public ADTGibtoniteStage Stage = ADTGibtoniteStage.Unstruck;

    [DataField]
    public int MinDetTime = 8;

    [DataField]
    public int MaxDetTime = 10;

    [DataField]
    public int DetTime;

    [DataField]
    public TimeSpan TickTime = TimeSpan.FromSeconds(0.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextTick;

    [DataField]
    public int HighQualityDetTime;

    [DataField]
    public int MediumQualityDetTime = 2;

    [DataField]
    public EntProtoId Ore = "ADTGibtonite";

    [DataField]
    public SoundSpecifier ActivateSound = new SoundPathSpecifier("/Audio/ADT/Mining/hit_on_shattered_glass.ogg");

    [DataField]
    public ADTBlastRadii StruckExplosion = new(1, 2, 5);

    [DataField]
    public ADTBlastRadii TimerExplosion = new(1, 3, 5);
}

[RegisterComponent]
public sealed partial class ADTActiveGibtoniteComponent : Component;

[Serializable, NetSerializable]
public enum ADTGibtoniteStage : byte
{
    Unstruck,
    Active,
    Stable,
    Detonated,
}

[Serializable, NetSerializable]
public enum ADTGibtoniteQuality : byte
{
    Low,
    Medium,
    High,
}

[Serializable, NetSerializable]
public enum ADTGibtoniteVisuals : byte
{
    Stage,
    Quality,
    Primed,
}
