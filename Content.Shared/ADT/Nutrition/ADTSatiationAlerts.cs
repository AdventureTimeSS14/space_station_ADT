using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Nutrition;

public static class ADTSatiationAlerts
{
    public static readonly ProtoId<AlertPrototype> HungerAlertId = "ADTHunger";
    public static readonly ProtoId<AlertPrototype> ThirstAlertId = "ADTThirst";

    public const int MaxLevel = 10;
    public const int FatLevel = 11;

    public static short ToLevel(float fraction)
    {
        return (short) MathF.Round(Math.Clamp(fraction, 0f, 1f) * MaxLevel);
    }
}