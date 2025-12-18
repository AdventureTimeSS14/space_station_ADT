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
    /// Минимальная инерция для работы, работает НЕ ПО ТАЙЛАМ В СЕКУНДУ, подробнее можно посмотреть в системе
    /// </summary>
    [DataField]
    public float MinVelocity = 21f; //ЭТО по идеи равняется 4.5 тайлам в секунду и я понятия не имею, каким чёртом оно работает
}
