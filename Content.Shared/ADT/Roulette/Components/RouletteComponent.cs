using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Roulette.Components;

/// <summary>
/// Компонент рулетки. Обрабатывает механику вращения, кулдауны и состояние.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RouletteComponent : Component
{
    /// <summary>
    /// Минимальная сила вращения рулетки.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float MinSpinForce = 500f;

    /// <summary>
    /// Максимальная сила вращения рулетки.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float MaxSpinForce = 1000f;

    /// <summary>
    /// Коэффициент трения для замедления вращения.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float Friction = 0.998f;

    [AutoNetworkedField]
    public float CurrentRotation = 0f;

    [AutoNetworkedField]
    public bool IsSpinning = false;

    [AutoNetworkedField]
    public bool CanSpin = true;

    /// <summary>
    /// Кулдаун между вращениями в секундах.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float SpinCooldown = 1f;

    [AutoNetworkedField]
    public float CurrentSpinSpeed = 0f;

    [AutoNetworkedField]
    public TimeSpan LastSpinTime = TimeSpan.Zero;
}
