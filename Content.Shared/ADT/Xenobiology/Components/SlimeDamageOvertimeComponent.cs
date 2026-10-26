using Content.Shared.Damage;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;

namespace Content.Shared.ADT.Xenobiology.Components;

/// <summary>
/// This is used for slime latching damage, this can be expanded in the future to allow for special breed dependent effects.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeDamageOvertimeComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? SourceEntityUid;

    /// <summary>
    /// How many units from target's bloodstream would be sucked per tick
    /// </summary>
    [DataField]
    public float SuctionUnits = 2.5f;

    /// <summary>
    /// What toxin would be injected inside target's bloodstream
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> ToxinReagent = "XenobioSlimeToxin";

    /// <summary>
    /// How many toxin units will be added to the targets bloodstream when eating the target
    /// </summary>
    [DataField]
    public float ToxinUnits = 0.15f;

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(2.0f);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTickTime = TimeSpan.Zero;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
        {
            { new("Caustic"), FixedPoint2.New(2.5f) },
        },
    };
}
