namespace Content.Server.ADT.Economy;

[RegisterComponent]
public sealed partial class StationCategorySurgeComponent : Component
{
    public Dictionary<string, float> Multipliers = new();
}

[RegisterComponent]
public sealed partial class CargoCategorySurgeRuleComponent : Component
{
    public EntityUid? Station;

    public readonly List<string> Categories = new();

    public int Percent;

    public bool Expensive;

    [DataField]
    public int MaxCategories = 3;

    [DataField]
    public int ExpensiveMinPercent = 50;

    [DataField]
    public int ExpensiveMaxPercent = 100;

    [DataField]
    public int CheapMinPercent = 25;

    [DataField]
    public int CheapMaxPercent = 50;
}
