using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(DynamicRuleSystem))]
public sealed partial class DynamicRuleComponent : Component
{
    [DataField]
    public int MaxEventsBeforeAntag = 9;
    public int EventsBeforeAntag = 9;

    public int Chaos = 0;
    public float TimeUntilNextEvent;
    [DataField]
    public int MinChaos = 10;
    [DataField]
    public int MaxChaos = 100;

    [DataField]
    public int MinEventDelay = 100;
    [DataField]
    public int MaxEventDelay = 600;
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
    [DataField(required: true)]
    public string Id;

    [DataField]
    public int Cost;
}
