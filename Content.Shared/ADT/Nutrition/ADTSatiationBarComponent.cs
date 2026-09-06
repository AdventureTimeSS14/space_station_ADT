using Content.Shared.Alert;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Nutrition;

[RegisterComponent]
public sealed partial class ADTSatiationBarComponent : Component
{
    public static readonly ProtoId<AlertPrototype> HungerAlertId = "ADTHunger";
    public static readonly ProtoId<AlertPrototype> ThirstAlertId = "ADTThirst";

    [DataField]
    public string BarStatePrefix = "bar";

    [DataField]
    public int MaxLevel = 22;

    [DataField]
    public float BarOffsetX = 8;

    [DataField]
    public Color FullColor = Color.CornflowerBlue;

    [DataField]
    public Color MediumColor = Color.Lime;

    [DataField]
    public Color LowColor = Color.Yellow;

    [DataField]
    public Color CriticalColor = Color.Red;

    [DataField]
    public float FullThreshold = 0.75f;

    [DataField]
    public float LowThreshold = 0.3f;

    [DataField]
    public float CriticalThreshold = 0.15f;

    [DataField]
    public Color FatColor = Color.Gray;
}

public enum ADTSatiationBarVisualLayers : byte
{
    Bar,
}