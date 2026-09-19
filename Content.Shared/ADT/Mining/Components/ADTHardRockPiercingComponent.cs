using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Mining.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTHardRockPiercingComponent : Component
{
    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
        {
            ["Blunt"] = 500,
        },
    };
}
