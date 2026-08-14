using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.AshWalker.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTHealTouchComponent : Component
{
    [DataField]
    public DamageSpecifier Healing = new()
    {
        DamageDict = new()
        {
            { "Blunt", -7 },
            { "Slash", -7 },
            { "Piercing", -6 },
            { "Heat", -20 },
            { "Poison", -10 },
            { "Asphyxiation", -50 },
        },
    };

    [DataField]
    public bool CanHealSelf = true;

    [DataField]
    public EntProtoId ActionId = "ADTActionHealTouch";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}
