using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.ADT.Body.Pulse;
using Content.Shared.Mobs;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.Surgery.UI;

public static class SurgeryConsoleStyle
{
    public const float PanelPadding = 10f;

    public static readonly Color Panel = Color.FromHex("#1A1A1E");
    public static readonly Color Track = Color.FromHex("#0E0E11");

    public static readonly Color Text = Color.FromHex("#E6E6EA");
    public static readonly Color TextDim = Color.FromHex("#9A9AA6");

    public static readonly Color Signal = Color.FromHex("#7CFF8A");

    public static readonly Color Healthy = Color.FromHex("#4CB050");
    public static readonly Color Bruised = Color.FromHex("#D9C53A");
    public static readonly Color Damaged = Color.FromHex("#E68A2E");
    public static readonly Color Failing = Color.FromHex("#EC5F55");

    public static readonly Color FailingTint = Color.FromHex("#2A1719");

    public static readonly Color CriticalBanner = Color.FromHex("#5A1F1F");
    public static readonly Color CriticalBannerText = Color.FromHex("#FFE3E0");
    public static readonly Color WarningBanner = Color.FromHex("#4A3214");
    public static readonly Color WarningBannerText = Color.FromHex("#FFE3B0");

    public static Color ForStage(OrganStage stage)
    {
        return stage switch
        {
            OrganStage.Healthy => Healthy,
            OrganStage.Bruised => Bruised,
            OrganStage.Damaged => Damaged,
            _ => Failing,
        };
    }

    public static Color ForBlood(float level)
    {
        return level switch
        {
            > 0.85f => Healthy,
            > 0.7f => Bruised,
            > 0.4f => Damaged,
            _ => Failing,
        };
    }

    public static Color ForDamage(float amount)
    {
        return amount switch
        {
            < 25f => Text,
            < 50f => Bruised,
            < 100f => Damaged,
            _ => Failing,
        };
    }

    public static Color ForMobState(MobState state)
    {
        return state switch
        {
            MobState.Alive => Healthy,
            MobState.SoftCritical => Bruised,
            MobState.Critical => Damaged,
            MobState.Dead => Failing,
            _ => TextDim,
        };
    }

    public static Color ForPulse(PulseReading pulse)
    {
        if (pulse.Strength == PulseStrength.Absent)
            return Failing;

        return pulse.Normal ? Signal : Bruised;
    }

    public static StyleBoxFlat Padded(Color background, float padding)
    {
        var box = new StyleBoxFlat(background);
        box.SetContentMarginOverride(StyleBox.Margin.All, padding);
        return box;
    }
}

public sealed class SurgeryConsolePanel : PanelContainer
{
    public SurgeryConsolePanel()
    {
        PanelOverride = SurgeryConsoleStyle.Padded(SurgeryConsoleStyle.Panel, SurgeryConsoleStyle.PanelPadding);
    }
}
