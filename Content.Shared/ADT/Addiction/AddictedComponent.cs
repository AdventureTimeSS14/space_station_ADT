namespace Content.Shared.ADT.Addiction.Components;

/// <summary>
/// Кароче, это не компонент как я понял, а затычка, для хранения данных. Нихуя не делает, добавлять никуда не нужно. используется для системы курения.
/// </summary>
[RegisterComponent]
public sealed partial class AddictedComponent : Component
{
    public List<LocId> PopupMessages = new();
    public TimeSpan NextPopup = TimeSpan.Zero;
}
