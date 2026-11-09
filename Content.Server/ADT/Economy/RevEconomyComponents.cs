using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Economy;

[RegisterComponent]
public sealed partial class StationSalaryCutComponent : Component
{
    public Dictionary<ProtoId<CargoAccountPrototype>, float> Cuts = new();

    [DataField]
    public float MaxCut = 0.9f;
}

[RegisterComponent]
public sealed partial class RevSalaryCutRuleComponent : Component
{
    /// <summary>
    /// Минимальное сокращение финансирования, %.
    /// </summary>
    [DataField]
    public int MinPercent = 5;

    /// <summary>
    /// Максимальное сокращение финансирования, %.
    /// </summary>
    [DataField]
    public int MaxPercent = 15;
}
