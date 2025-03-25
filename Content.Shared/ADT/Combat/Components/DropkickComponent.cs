using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Combat;

[RegisterComponent, NetworkedComponent]
public sealed partial class DropkickComponent : Component
{
    [DataField]
    public List<CombatMove> AvailableMoves { get; private set; } = new List<CombatMove>();
    public EntityUid TargetEntity;
    public List<CombatAction> CurrestActions { get; private set; } = new List<CombatAction>();
}
