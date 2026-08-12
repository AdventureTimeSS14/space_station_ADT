using Robust.Shared.GameStates;

namespace Content.Shared.ADT.DrunkDrift;

/// <summary>
///     Маркер пьяного существа для системы шатания и падений (ADT).
///     Ставится и снимается сервером по событиям статус-эффекта опьянения.
///     Клиенту нужен, чтобы предсказывать покачивание движения.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ADTDrunkDriftComponent : Component
{
    /// <summary>
    ///     Время, до которого пьяный не может упасть повторно (кулдаун падений, сервер).
    /// </summary>
    [DataField]
    public TimeSpan NextFall;

    /// <summary>
    ///     Пьяные эффекты активны (остаток опьянения >= порога размытия экрана).
    ///     Считает сервер, поле сетевое: и клиент (шатание), и сервер (падения) видят одно значение.
    /// </summary>
    [DataField]
    public bool VisualsActive;
}
