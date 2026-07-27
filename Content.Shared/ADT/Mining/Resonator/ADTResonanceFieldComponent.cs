using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Mining.Resonator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ADTResonanceFieldComponent : Component
{
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? BurstTime;

    [DataField]
    public TimeSpan AutoDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan Lifetime = TimeSpan.FromSeconds(60);

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public float DamageMultiplier = 1f;

    [DataField]
    public float LowPressureMultiplier = 3f;

    [DataField]
    public float LowPressureThreshold = 50f;

    [DataField]
    public EntityUid? Creator;

    [DataField]
    public EntityUid? Resonator;

    [DataField, AutoNetworkedField]
    public ADTResonatorMode Mode = ADTResonatorMode.Auto;

    [DataField]
    public float FailureProbability;

    [DataField]
    public float AddedFailure = 0.5f;

    [DataField]
    public float SpreadRange = 1.5f;

    [ViewVariables]
    public bool Bursting;

    [DataField]
    public EntProtoId? BurstEffect = "ADTResonanceCrushEffect";

    [DataField]
    public SoundSpecifier? BurstSound = new SoundPathSpecifier("/Audio/ADT/Weapons/Resonator/resonator_blast.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f),
    };
}

[Serializable, NetSerializable]
public enum ADTResonanceFieldVisuals : byte
{
    Mode,
}
