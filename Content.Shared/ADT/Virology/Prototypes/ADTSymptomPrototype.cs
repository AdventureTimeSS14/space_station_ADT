using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Virology.Prototypes;

[Prototype("adtSymptom")]
public sealed partial class ADTSymptomPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LocId? Description;

    [DataField]
    public int Stealth;

    [DataField]
    public int Resistance;

    [DataField]
    public int StageSpeed;

    [DataField]
    public int Transmittable;

    [DataField]
    public int Level;

    [DataField]
    public int Severity;

    [DataField]
    public bool Generatable = true;

    [DataField]
    public bool Passive;

    [DataField]
    public List<ADTDiseaseStageEffects> Stages = new();

    [DataField]
    public EntityEffect[]? OnStart;

    [DataField]
    public EntityEffect[]? OnEnd;

    [DataField]
    public List<ADTSymptomDiseaseEffect> DiseaseEffects = new();
}

[DataDefinition]
public sealed partial class ADTSymptomDiseaseEffect
{
    [DataField]
    public int MinStage = 1;

    [DataField]
    public int MaxStage = int.MaxValue;

    [DataField]
    public float Probability = 1f;

    [DataField]
    public ADTSymptomPhase Phase = ADTSymptomPhase.Activate;

    [DataField(required: true)]
    public ADTSymptomEffect Effect = default!;
}
