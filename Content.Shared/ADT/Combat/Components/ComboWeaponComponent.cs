using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Combat;

[RegisterComponent, NetworkedComponent]
public sealed partial class ComboWeaponComponent : Component
{
    [DataField]
    public List<ComboWeaponMove> AvailableMoves { get; private set; } = new List<ComboWeaponMove>();
    public EntityUid TargetEntity;
    public List<WeaponCombatAction> CurrestActions { get; private set; } = new List<WeaponCombatAction>();
    public EntityUid Target = default;
    public ComboWeaponStand CurrentStand = ComboWeaponStand.Offensive;
}
