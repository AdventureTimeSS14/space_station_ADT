using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.ADT.Body;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class OrganTraumaComponent : Component
{
    [DataField]
    public List<ProtoId<OrganCategoryPrototype>> RadiationOrgans = new() { "Heart", "Lungs", "Liver", "Kidneys", "Stomach", "Brain", "Eyes" };

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public float ElectrocutionHeartDamage = 4f;

    [DataField]
    public float MinBreathMoles = 0.001f;

    [DataField]
    public float HotBreathTemperature = 360f;

    [DataField]
    public float ColdBreathTemperature = 180f;

    [DataField]
    public float BreathBurnDamage = 0.4f;

    [DataField]
    public float HypoxiaThreshold = 50f;

    [DataField]
    public float HypoxiaBrainDamage = 0.4f;

    [DataField]
    public float PoisonThreshold = 40f;

    [DataField]
    public float PoisonOrganDamage = 0.3f;

    [DataField]
    public float RadiationThreshold = 30f;

    [DataField]
    public float RadiationOrganDamage = 0.4f;

    [DataField]
    public float BarotraumaLungsDamage = 0.5f;

    [DataField]
    public float BarotraumaLungsMaxDamage = 0.9f;

    [DataField]
    public float BarotraumaEarsDamage = 0.25f;
}
