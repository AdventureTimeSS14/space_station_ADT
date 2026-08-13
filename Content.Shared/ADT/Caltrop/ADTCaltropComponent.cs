using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Caltrop;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTCaltropComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public float MinMultiplier = 1f;

    [DataField]
    public float MaxMultiplier = 2f;

    [DataField]
    public float Probability = 0.7f;

    [DataField]
    public TimeSpan ParalyzeTime = TimeSpan.FromSeconds(6);

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public LocId PopupSelf = "adt-caltrop-step-self";

    [DataField]
    public LocId PopupOthers = "adt-caltrop-step-others";

    [DataField]
    public TimeSpan MessageCooldown = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextMessage;
}
