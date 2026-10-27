namespace Content.Shared.ADT.Surgery.Components;

[RegisterComponent]
public sealed partial class SurgeryConsoleComponent : Component
{
    [DataField]
    public bool AlertsEnabled = true;

    [DataField]
    public TimeSpan AlertCooldown = TimeSpan.FromSeconds(45);

    [DataField]
    public float LowBloodThreshold = 0.6f;

    public TimeSpan NextCheck;

    public Dictionary<SurgeryConsoleAlert, TimeSpan> NextAlert = [];
}

public enum SurgeryConsoleAlert : byte
{
    Dead,
    Critical,
    LowBlood,
    Bleeding,
}
