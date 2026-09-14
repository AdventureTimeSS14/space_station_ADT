namespace Content.Shared.ADT.GPS;

[RegisterComponent]
public sealed partial class GpsSignalComponent : Component
{
    [DataField(required: true)]
    public string Tag = string.Empty;

    [DataField]
    public string? Description;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public bool SameMapOnly;

    [DataField]
    public bool Enabled = true;

    [DataField]
    public bool DisableOnDeath = true;
}
