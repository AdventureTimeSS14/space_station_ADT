namespace Content.Shared.ADT.Addiction.AddictedComponent;

/// <summary>
/// Компонент для базовых представителей рас.
/// Используется такие переменные для указания: RequiredTime, ChangeAddictionTypeTime.
/// </summary>
[RegisterComponent]
public sealed partial class AddictedComponent : Component
{
    //public List<LocId> PopupMessages = new();
    //public TimeSpan NextPopup = TimeSpan.Zero;
    public TimeSpan CurrentAddictedTime = TimeSpan.Zero; // Время проведенное за употреблением - время, когда не употреблял.
    [DataField(required: true)] public float ChangeAddictionTypeTime; // Время от которого зависит, как скоро сменится тип зависимости на более тяжелую.
    public float RemaningTime; // Время оставшиеся до того, как сменится тип зависимости.
    [DataField(required: true)] public TimeSpan RequiredTime; // Время необходимое, чтобы стать зависимым.
    public int TypeAddiction; // Собсна тип зависимости. 0 - легкая, без побочек, 1 - с небольшими побочками и т п, до 4
    public bool Addicted = false; // собсна сообщает нам имеет ли зависимость пациент
    public TimeSpan LastEffect = TimeSpan.Zero; // Собсна время, которое сообщает нам, время последнего воздействия эффекта на челика
}
