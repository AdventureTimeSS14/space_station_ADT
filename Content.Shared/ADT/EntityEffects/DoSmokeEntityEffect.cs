using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;

public sealed partial class DoSmokeEntityEffect : EntityEffectBase<DoSmokeEntityEffect>
{
    [DataField]
    public float Duration = 10;

    [DataField(required: true)]
    public int SpreadAmount = 5;

    [DataField]
    public EntProtoId SmokePrototype = "Smoke";

    [DataField]
    public Solution Solution = new();

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override LogImpact? Impact => LogImpact.Medium;
}