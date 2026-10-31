using Content.Shared.Nutrition.Components;
using Content.Shared.Tools;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Surgery.Prototypes;

[Prototype]
public sealed partial class SurgeryToolQualityPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public List<SurgeryToolQualityEntry> Entries = new();
}

[DataDefinition]
public sealed partial class SurgeryToolQualityEntry
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public ProtoId<ToolQualityPrototype>? ToolQuality;

    [DataField]
    public UtensilType Utensil = UtensilType.None;

    [DataField]
    public bool RequireHot;

    [DataField]
    public float Speed = 1f;

    [DataField]
    public float Success = 1f;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public List<SurgeryStepEffect> FailureEffects = new();

    public bool HasCriteria => Whitelist != null || ToolQuality != null || Utensil != UtensilType.None || RequireHot;
}
