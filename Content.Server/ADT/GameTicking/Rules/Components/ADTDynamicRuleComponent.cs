using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ADTDynamicRuleSystem))]
[AutoGenerateComponentPause]
public sealed partial class DynamicRuleComponent : Component
{
    /// <summary>
    /// The total budget for antags.
    /// </summary>
    [DataField]
    public float Budget;

    /// <summary>
    /// The last time budget was updated.
    /// </summary>
    [DataField]
    public TimeSpan LastBudgetUpdate;

    /// <summary>
    /// The amount of budget accumulated every second.
    /// </summary>
    [DataField]
    public float BudgetPerSecond = 0.1f;
    
    /// <summary>
    /// A table of rules that are picked from.
    /// </summary>
    [DataField]
    public EntityTableSelector Table = new NoneSelector();

    /// <summary>
    /// The rules that have been spawned
    /// </summary>
    [DataField]
    public List<EntityUid> Rules = new();

    /// <summary>
    /// The time at which the next rule will start
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRuleTime;

    /// <summary>
    /// Minimum delay between rules
    /// </summary>
    [DataField]
    public TimeSpan MinRuleInterval = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Maximum delay between rules
    /// </summary>
    [DataField]
    public TimeSpan MaxRuleInterval = TimeSpan.FromMinutes(30);

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
