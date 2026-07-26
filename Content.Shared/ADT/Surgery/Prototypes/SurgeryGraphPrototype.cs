using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared.Tag;

namespace Content.Shared.ADT.Surgery.Prototypes;

[Prototype]
public sealed partial class SurgeryGraphPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public ProtoId<SurgeryCategoryPrototype>? Category;

    [DataField]
    public List<string> StartNodes = new();

    [DataField("nodes")]
    public List<SurgeryGraphNode> Nodes = new();
}

[Prototype]
public sealed partial class SurgeryCategoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public ProtoId<SurgeryCategoryPrototype>? Parent;
}

[DataDefinition]
public sealed partial class SurgeryGraphNode
{
    [DataField(required: true)]
    public string Name = default!;

    [DataField]
    public string Label = string.Empty;

    [DataField]
    public List<SurgeryGraphEdge> Edges = new();

    [DataField]
    public List<ProtoId<SurgeryEdgePackagePrototype>> Packages = new();
}

[DataDefinition]
public sealed partial class SurgeryGraphEdge
{
    [DataField(required: true)]
    public string Id = default!;

    [DataField(required: true)]
    public string Target = default!;

    [DataField]
    public string Label = string.Empty;

    [DataField]
    public SpriteSpecifier? Icon;

    [DataField]
    public List<SurgeryStepEntry> Steps = new();

    [DataField]
    public List<SurgeryStepCondition> Conditions = new();

    [DataField]
    public List<SurgeryStepEffect> Effects = new();
}

[Prototype]
public sealed partial class SurgeryEdgePackagePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("edges", required: true)]
    public List<SurgeryGraphEdge> Edges = new();
}

[DataDefinition]
public sealed partial class SurgeryStepEntry
{
    [DataField(required: true)]
    public string Name = default!;

    [DataField]
    public List<ProtoId<TagPrototype>> Tools = new();

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2);

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public float SuccessChance = 1f;

    [DataField]
    public List<SurgeryStepEffect> FailureEffects = new();

    [DataField]
    public List<SurgeryStepEffect> Effects = new();
}
