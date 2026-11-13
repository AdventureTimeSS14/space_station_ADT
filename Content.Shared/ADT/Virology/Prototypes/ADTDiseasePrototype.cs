using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.ADT.Virology.Prototypes;

[Prototype("adtDisease")]
public sealed partial class ADTDiseasePrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ADTDiseasePrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField, NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField]
    public LocId Name = "adt-disease-unknown";

    [DataField]
    public LocId? Description;

    [DataField]
    public LocId Form = "adt-disease-form-disease";

    [DataField]
    public LocId Agent = "adt-disease-agent-default";

    [DataField]
    public LocId? CureText;

    [DataField]
    public int MaxStages = 5;

    [DataField]
    public float StageProb = 2f;

    [DataField]
    public ADTDiseaseVisibility Visibility = ADTDiseaseVisibility.Visible;

    [DataField]
    public ADTDiseaseSeverity Severity = ADTDiseaseSeverity.Nonthreat;

    [DataField]
    public float DiscoveryThreshold = 0.5f;

    [DataField]
    public bool Curable = true;

    [DataField]
    public List<ProtoId<ReagentPrototype>> Cures = new();

    [DataField]
    public bool NeedsAllCures = true;

    [DataField]
    public float CureProb = 8f;

    [DataField]
    public bool CanImmunity = true;

    [DataField]
    public bool IgnoreImmunity;

    [DataField]
    public bool VirusHealResistant;

    [DataField]
    public bool RestCure;

    [DataField]
    public bool BindsMaster;

    [DataField]
    public List<ProtoId<ADTDiseasePrototype>> RequiredResistances = new();

    [DataField]
    public float MissingResistanceCureProb = 1f;

    [DataField]
    public LocId? CuredMessage = "adt-disease-cured-default";

    [DataField]
    public float MutationChance = 1f;

    [DataField]
    public List<ProtoId<ReagentPrototype>> MutationReagents = new() { "UnstableMutagen" };

    [DataField]
    public List<ProtoId<ADTDiseasePrototype>> PossibleMutations = new();

    [DataField]
    public EntityWhitelist? Infectable;

    [DataField]
    public EntityWhitelist? CarrierOnly;

    [DataField]
    public bool CanProgressInDead;

    [DataField]
    public bool CanContractDead;

    [DataField]
    public float SpreadFromDeadProb;

    [DataField]
    public bool IsVirus;

    [DataField]
    public ADTDiseaseSpread Spread = ADTDiseaseSpread.NonContagious;

    [DataField]
    public float Infectivity = 65f;

    [DataField]
    public float Permeability = 1f;

    [DataField]
    public bool NoVaccine;

    [DataField]
    public List<ADTDiseaseStageEffects> Stages = new();

    [DataField]
    public List<ProtoId<ADTSymptomPrototype>> Symptoms = new();
}

[DataDefinition]
public sealed partial class ADTDiseaseStageEffects
{
    [DataField]
    public int MinStage = 1;

    [DataField]
    public int MaxStage = int.MaxValue;

    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}
