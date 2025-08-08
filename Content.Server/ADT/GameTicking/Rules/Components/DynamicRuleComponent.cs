using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(DynamicRuleSystem))]
public sealed partial class DynamicRuleComponent : Component
{
    [DataField]
    public int MaxEventsBeforeAntag = 9;
    [ViewVariables(VVAccess.ReadWrite)]
    public int EventsBeforeAntag = 9;
    [ViewVariables(VVAccess.ReadWrite)]
    public int Chaos = 0;
    [ViewVariables(VVAccess.ReadWrite)]
    public float TimeUntilNextEvent = 100;
    [DataField]
    public int MinChaos = 10;
    [DataField]
    public int MaxChaos = 100;

    [DataField]
    public int MinEventDelay = 150;
    [DataField]
    public int MaxEventDelay = 700;
    /// <summary>
    /// Раундстартовые геймрулы
    /// </summary>
    [DataField]
    public HashSet<DynamicRulePriced> RoundstartRules = new();

    [DataField]
    public HashSet<string> AddedRules = new();
    /// <summary>
    /// тут должен быть простой шкедуллер
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector ScheduledGameRules = default!;

    /// <summary>
    /// мидраунд антаги
    /// ДОЛЖНЫ ИДТИ СТРОГО ОТ САМОГО ДОРОГОГО К САМОМУ ДЕШЕВОМУ
    /// </summary>
    [DataField]
    public HashSet<DynamicRulePriced> MidroundAntags = new();
}

[DataDefinition]
[Serializable]
public sealed partial record DynamicRulePriced
{
    /// <summary>
    /// тут должно быть исключительно айди геймрула по типу HereticDuoGameRule
    /// </summary>
    [DataField(required: true)]
    public string Id;

    /// <summary>
    /// больше кост = меньше шанс на антага
    /// </summary>
    [DataField]
    public int Cost;
    /// <summary>
    /// шанс с которым этот геймрул рерольнит
    /// </summary>
    [DataField]
    public double RerollChance = 0;
}
