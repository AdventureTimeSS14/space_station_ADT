using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Combat;

[RegisterComponent, NetworkedComponent]
public sealed partial class ComboComponent : Component
{
    [DataField]
    public List<CombatMove> AvailableMoves { get; private set; } = new List<CombatMove>();
    public EntityUid TargetEntity;
    public List<CombatAction> CurrestActions { get; private set; } = new List<CombatAction>();
    public EntityUid Target = default;
}
