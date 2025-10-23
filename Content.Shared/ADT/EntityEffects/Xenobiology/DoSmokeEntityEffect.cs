using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects;

/// <summary>
///     Creates smoke similar to SmokeOnTrigger
/// </summary>
public sealed partial class DoSmokeEntityEffect : EventEntityEffect<DoSmokeEntityEffect>
{

    /// <summary>
    /// How long the smoke stays for, after it has spread.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Duration = 10;

    /// <summary>
    /// How much the smoke will spread.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public int SpreadAmount;

    /// <summary>
    /// Smoke entity to spawn.
    /// Defaults to smoke but you can use foam if you want.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId SmokePrototype = "Smoke";

    /// <summary>
    /// Solution to add to each smoke cloud.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Solution Solution = new();

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override LogImpact LogImpact => LogImpact.Medium;
}
