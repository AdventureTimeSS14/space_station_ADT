namespace Content.Shared.ADT.Combat;

/// <summary>
/// Комбосчётчик для SharedComboSystem
/// </summary>
[RegisterComponent]
public sealed partial class ComboCounterComponent : Component
{
    [DataField("comboCounter")]
    public int ComboCounter;

    [DataField("maxCombo")]
    public int MaxCombo;

    [DataField("lastCombo")]
    public TimeSpan LastCombo;

    [DataField("duration")]
    public float Duration = 6f;

}
