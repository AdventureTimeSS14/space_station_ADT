using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Silicon.Components;

/// <summary>
///     Универсальный компонент ЭМИ помех для всех типов игроков.
///     Добавляет эффект помех на экран клиента.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmpInterferenceComponent : Component
{
    /// <summary>
    ///     Множитель интенсивности эффекта.
    ///     Установка 0.5 уменьшит эффект вдвое, 2.0 - удвоит.
    /// </summary>
    [AutoNetworkedField]
    public float Multiplier = 1f;

    /// <summary>
    ///     Длительность эффекта в секундах.
    /// </summary>
    [AutoNetworkedField]
    public float Duration = 10f;
}
