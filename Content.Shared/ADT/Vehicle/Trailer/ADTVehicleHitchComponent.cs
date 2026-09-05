using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Vehicle.Trailer;

/// <summary>
/// Компонент транспорта со сцепкой: создаёт дочернюю сущность-сцепку, к которой цепляются прицепы.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ADTVehicleHitchComponent : Component
{
    /// <summary>
    /// Прототип создаваемой сцепки.
    /// </summary>
    [DataField]
    public EntProtoId HitchPrototype = "ADTVehicleHitch";

    /// <summary>
    /// Смещение сцепки относительно транспорта.
    /// </summary>
    [DataField]
    public Vector2 HitchOffset = new(0, 0.55f);

    /// <summary>
    /// Радиус поиска прицепа вокруг сцепки.
    /// </summary>
    [DataField]
    public float AttachRange = 2.5f;

    /// <summary>
    /// Действие водителя: прицепить или отцепить прицеп.
    /// </summary>
    [DataField]
    public EntProtoId ToggleAction = "ADTActionTrailerToggle";

    /// <summary>
    /// Созданная сцепка.
    /// </summary>
    public EntityUid? Hitch;

    /// <summary>
    /// Созданное действие прицепа.
    /// </summary>
    public EntityUid? ToggleActionEntity;
}