using Robust.Shared.GameStates;

namespace Content.Shared.ADT.DrunkDrift;

/// <summary>
///     Маркер пьяного существа для системы шатания и падений (ADT).
///     Ставится и снимается сервером по событиям статус-эффекта опьянения.
///     Клиенту нужен, чтобы предсказывать покачивание движения.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTDrunkDriftComponent : Component
{
    /// <summary>
    ///     Время, до которого пьяный не может упасть повторно (кулдаун падений, сервер).
    /// </summary>
    [DataField]
    public TimeSpan NextFall;

    /// <summary>
    ///     Пьяные эффекты активны, пока остаток опьянения не ниже порога размытия экрана.
    ///     Считает сервер, поле сетевое: и клиент (шатание), и сервер (падения) видят одно значение.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool VisualsActive;

    /// <summary>
    ///     Порог остатка опьянения (сек), с которого работают пьяные эффекты.
    ///     Совпадает с порогом размытия экрана в DrunkOverlay (50 сек).
    /// </summary>
    [DataField]
    public TimeSpan VisualThreshold = TimeSpan.FromSeconds(50);

    /// <summary>
    ///     Шанс споткнуться и упасть за секунду движения (0..1).
    /// </summary>
    [DataField]
    public float FallChance = 0.025f;

    /// <summary>
    ///     Кулдаун между падениями.
    /// </summary>
    [DataField]
    public TimeSpan FallCooldown = TimeSpan.FromSeconds(12);

    /// <summary>
    ///     Длительность нокдауна при падении.
    /// </summary>
    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(1.75);

    /// <summary>
    ///     Амплитуда плавного покачивания (радианы). Сетевое: шатание считает клиент.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SwayAmplitude = 0.12f;

    /// <summary>
    ///     Шанс рывка в сторону на каждом интервале (0..1). Сетевое: шатание считает клиент.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LurchChance = 0.35f;

    /// <summary>
    ///     Сила рывка (радианы). Сетевое: шатание считает клиент.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LurchAngle = 0.25f;

    /// <summary>
    ///     Длина интервала рывков (сек). Сетевое: шатание считает клиент.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LurchInterval = 3f;
}
