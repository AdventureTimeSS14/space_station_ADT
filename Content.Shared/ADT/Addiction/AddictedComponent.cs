namespace Content.Shared.ADT.Addiction.AddictedComponent;

/// <summary>
/// Компонент для базовых представителей рас.
/// Используется такие переменные для указания: RequiredTime, ChangeAddictionTypeTime.
/// </summary>
[RegisterComponent]
public sealed partial class AddictedComponent : Component
{
    /// <summary>
    /// Время проведенное за употреблением - время, когда не употреблял.
    /// </summary>
    [DataField] public TimeSpan CurrentAddictedTime = TimeSpan.Zero;
    /// <summary>
    /// Время проведенное за употреблением - время, когда не употреблял.
    /// </summary>
    [DataField(required: true)] public TimeSpan ChangeAddictionTypeTime;
    /// <summary>
    /// Время оставшиеся до того, как сменится тип зависимости.
    /// </summary>
    [DataField] public TimeSpan RemaningTime;
    /// <summary>
    /// Время необходимое, чтобы стать зависимым.
    /// </summary>
    [DataField(required: true)] public TimeSpan RequiredTime;
    /// <summary>
    /// Собсна тип зависимости. 0 - легкая, без побочек, 1 - с небольшими побочками и т п, до 4
    /// </summary>
    [DataField] public int TypeAddiction;
    /// <summary>
    /// собсна сообщает нам имеет ли зависимость пациент
    /// </summary>
    [DataField] public bool Addicted = false;
    /// <summary>
    /// Собсна время, которое сообщает нам, время последнего воздействия эффекта на челика
    /// </summary>
    [DataField] public TimeSpan LastEffect = TimeSpan.Zero;
}
