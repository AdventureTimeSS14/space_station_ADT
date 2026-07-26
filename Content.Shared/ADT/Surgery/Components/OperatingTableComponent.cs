namespace Content.Shared.ADT.Surgery.Components;

[RegisterComponent]
public sealed partial class OperatingTableComponent : Component
{
    [DataField]
    public float Modifier = 1f;
}
