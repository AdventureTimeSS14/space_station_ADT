using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Mining.Resonator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ADTResonatorComponent : Component
{
    [DataField]
    public EntProtoId FieldProto = "ADTResonanceField";

    [DataField]
    public List<ADTResonatorMode> Modes = new()
    {
        ADTResonatorMode.Auto,
        ADTResonatorMode.Manual,
    };

    [DataField, AutoNetworkedField]
    public ADTResonatorMode Mode = ADTResonatorMode.Auto;

    [DataField]
    public int FieldLimit = 4;

    [DataField]
    public float ChargeUsePercent = 0.1f;

    [DataField]
    public TimeSpan PlantDelay = TimeSpan.FromSeconds(1.2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextPlant;

    [DataField]
    public float QuickBurstModifier = 0.8f;

    [DataField]
    public float AddedFailure = 0.5f;

    [ViewVariables]
    public HashSet<EntityUid> Fields = new();

    [DataField]
    public SoundSpecifier? PlantSound = new SoundPathSpecifier("/Audio/ADT/Weapons/Resonator/resonator_fire.ogg")
    {
        Params = AudioParams.Default.WithVolume(-4f),
    };
}

[Serializable, NetSerializable]
public enum ADTResonatorMode : byte
{
    Auto,
    Manual,
    Matrix,
}
