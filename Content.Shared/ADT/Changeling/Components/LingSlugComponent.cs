using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

[RegisterComponent]
[AutoGenerateComponentState(true)]
public sealed partial class LingSlugComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LayingDuration = TimeSpan.FromSeconds(15);

    [DataField]
    public bool EggsLaid = false;

    [DataField("chemicalToxin", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string ChemicalToxin = "Toxin";

    [ViewVariables(VVAccess.ReadWrite), DataField("toxinAmount")]
    public float ToxinAmount = 50f;

    [DataField]
    public EntProtoId LayEggsAction = "ActionLingEggs";

    [DataField, AutoNetworkedField]
    public EntityUid? LayEggsActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? EggLing;

    [DataField]
    public float LayedDamage = 70;

    [DataField]
    public float AbsorbedDnaModifier = 0f;

    [DataField("spread")]
    public bool Spread = false;

    [DataField("eggsAction")]
    public EntProtoId HatchAction = "ActionLingHatch";

    [DataField]
    public bool EggsReady = false;

    [DataField]
    public float GibDamage = 5000f;
}
