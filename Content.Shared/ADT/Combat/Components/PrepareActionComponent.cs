using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Combat;

[RegisterComponent, NetworkedComponent]
public sealed partial class PrepareActionComponent : Component
{
    [DataField]
    public List<IComboEffect>? PreparedMove;

    [DataField]
    public List<EntProtoId> BaseCombatMoves = new()
    {
        "ActionLegSweep",
        "ActionLungBlock",
        "ActionNeckChop"
    };

    public readonly List<EntityUid> CombatMoveEntities = new()
    {
    };
    [DataField]
    public bool CanBeUsedWithCombo;
}

