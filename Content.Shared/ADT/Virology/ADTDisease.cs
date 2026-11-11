using System.Linq;
using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.ADT.Virology;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ADTDisease
{
    [ViewVariables]
    public string Id => GetId();

    [DataField]
    public ProtoId<ADTDiseasePrototype> Prototype = "ADTAdvanceVirus";

    [DataField]
    public List<ProtoId<ADTSymptomPrototype>> Symptoms = new();

    [DataField]
    public string? Name;

    [DataField]
    public int Stage = 1;

    [DataField]
    public int MaxStages = 5;

    [DataField]
    public float StageProb = 2f;

    [DataField]
    public float CureProb = 8f;

    [DataField]
    public List<ProtoId<ReagentPrototype>> Cures = new();

    [DataField]
    public bool NeedsAllCures = true;

    [DataField]
    public ADTDiseaseSeverity Severity = ADTDiseaseSeverity.Nonthreat;

    [DataField]
    public ADTDiseaseVisibility Visibility = ADTDiseaseVisibility.Visible;

    [DataField]
    public ADTDiseaseSpread Spread = ADTDiseaseSpread.NonContagious;

    [DataField]
    public float Permeability = 1f;

    [DataField]
    public float MutationChance = 1f;

    [DataField]
    public List<ProtoId<ReagentPrototype>> MutationReagents = new();

    [DataField]
    public List<ProtoId<ADTDiseasePrototype>> PossibleMutations = new();

    [DataField]
    public bool Carrier;

    [DataField]
    public bool Discovered;

    [DataField]
    public NetEntity? Master;

    [DataField]
    public int MasterTimer;

    [NonSerialized]
    public bool SymptomsStarted;

    [NonSerialized]
    public List<ADTSymptomState>? SymptomStates;

    [NonSerialized]
    public TimeSpan NextStageTime;

    public bool IsAdvance => Symptoms.Count > 0;

    public string GetId()
    {
        if (!IsAdvance)
            return Prototype;

        var ids = Symptoms.Select(s => s.Id).ToList();
        ids.Sort(string.CompareOrdinal);
        return string.Join(":", ids);
    }

    public bool IsSame(ADTDisease other)
    {
        if (IsAdvance != other.IsAdvance)
            return false;

        return GetId() == other.GetId();
    }

    public ADTDisease Copy()
    {
        return new ADTDisease
        {
            Prototype = Prototype,
            Symptoms = new List<ProtoId<ADTSymptomPrototype>>(Symptoms),
            Name = Name,
            Stage = 1,
            MaxStages = MaxStages,
            StageProb = StageProb,
            CureProb = CureProb,
            Cures = new List<ProtoId<ReagentPrototype>>(Cures),
            NeedsAllCures = NeedsAllCures,
            Severity = Severity,
            Visibility = Visibility,
            Spread = Spread,
            Permeability = Permeability,
            MutationChance = MutationChance,
            MutationReagents = new List<ProtoId<ReagentPrototype>>(MutationReagents),
            PossibleMutations = new List<ProtoId<ADTDiseasePrototype>>(PossibleMutations),
            Master = Master,
        };
    }

    public static ADTDisease FromPrototype(ADTDiseasePrototype proto)
    {
        return new ADTDisease
        {
            Prototype = proto.ID,
            MaxStages = proto.MaxStages,
            StageProb = proto.StageProb,
            CureProb = proto.CureProb,
            Cures = new List<ProtoId<ReagentPrototype>>(proto.Cures),
            NeedsAllCures = proto.NeedsAllCures,
            Severity = proto.Severity,
            Visibility = proto.Visibility,
            Spread = proto.Spread,
            Permeability = proto.Permeability,
            MutationChance = proto.MutationChance,
            MutationReagents = new List<ProtoId<ReagentPrototype>>(proto.MutationReagents),
            PossibleMutations = new List<ProtoId<ADTDiseasePrototype>>(proto.PossibleMutations),
        };
    }
}
