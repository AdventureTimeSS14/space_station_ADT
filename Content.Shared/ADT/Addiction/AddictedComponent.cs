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
    public int CurrentAddictedTime; // Время проведенное за употреблением - время, когда не употреблял.
    public int RemaningTime; // Время оставшиеся до того, как сменится тип зависимости.
    public int ChangeAddictionTypeTime; // Время от которого зависит, как скоро сменится тип зависимости на более тяжелую.
    public int RequiredTime; // Время необходимое, чтобы стать зависимым.
    public int TypeAddiction; // Собсна тип зависимости. 0 - легкая, без побочек, 1 - с небольшими побочками и т п, до 4
    public bool Addicted = false; // собсна сообщает нам имеет ли зависимость пациент
}
