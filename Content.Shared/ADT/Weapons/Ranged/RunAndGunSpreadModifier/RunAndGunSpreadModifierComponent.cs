using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Weapons.Ranged.RunAndGunSpreadModifier;

[RegisterComponent, NetworkedComponent]
public sealed partial class RunAndGunSpreadModifierComponent : Component
{
    /// <summary>
    /// множитель скорости разброса
    /// </summary>
    [DataField]
    public float Modifier = 1f;
    /// <summary>
    /// Минимальная инерция для работы, работает по тайлам в секунду
    /// </summary>
    [DataField]
    public float MinVelocity = 4.6f;
}
