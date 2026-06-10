using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.MartialArts;

[Prototype("combo")]
public sealed partial class ComboPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public MartialArtsForms MartialArtsForm;

    [DataField("attacks", required: true)]
    public List<ComboAttackType> AttackTypes = new();

    [DataField("event", required: true)]
    public object? ResultEvent;

    [DataField]
    public float ExtraDamage;

    [DataField]
    public int ParalyzeTime;

    [DataField]
    public bool CanDoWhileProne = true;

    [DataField]
    public bool DropItems = false;

    [DataField]
    public float StaminaDamage;

    [DataField]
    public string DamageType = "Blunt";

    [DataField]
    public float ThrownSpeed = 7f;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField]
    public bool PerformOnSelf;
}

[Prototype("comboList")]
public sealed partial class ComboListPrototype : IPrototype
{
    [IdDataField] public string ID { get; private init; } = default!;

    [DataField(required: true)]
    public List<ProtoId<ComboPrototype>> Combos = new();
}
