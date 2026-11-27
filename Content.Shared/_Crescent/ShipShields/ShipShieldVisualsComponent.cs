using System.Numerics; // LuaM
using Robust.Shared.GameStates;

namespace Content.Shared._Crescent.ShipShields;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause] // LuaM: added AutoGenerateComponentPause
public sealed partial class ShipShieldVisualsComponent : Component
{
    /// <summary>
    /// The color of this shield.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color ShieldColor = Color.FromHex("#00AAFF").WithAlpha(0.92f); // LuaM: Color.White > translucent blue

    /// <summary>
    /// The extra padding of this shield.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Padding = 50f;

    // LuaM-start: animated shader parameters ported from Sector Frontier.
    [ViewVariables, AutoNetworkedField, AutoPausedField] // LuaM: added AutoPausedField
    public TimeSpan? FormStart; // LuaM: float Form > TimeSpan? FormStart

    [ViewVariables, AutoNetworkedField, AutoPausedField] // LuaM: added AutoPausedField
    public TimeSpan? ShatterStart; // LuaM: float Shatter > TimeSpan? ShatterStart

    [DataField]
    public float SpinupTime = 1.25f;

    [DataField]
    public float ShatterTime = 1.0f;

    [DataField]
    public float Brightness = 1.15f;

    [DataField]
    public float PixelGrid = 1f;

    [DataField]
    public float HexDensity = 14f;

    [DataField]
    public float CoreFade = 0.83f;

    [DataField]
    public float FillLevel = 0.12f;

    [DataField]
    public float LineLevel = 0.48f;

    [DataField]
    public float RimLevel = 0.8f;

    [DataField]
    public float AlphaBands = 6f;

    [DataField]
    public float BreathDepth = 0.1f;

    [DataField]
    public Vector2 FormOrigin = Vector2.Zero;

    [DataField]
    public float ShardScale = 5f;
}

public static class ShipShieldVisualsProgress
{
    public static bool IsVisible(ShipShieldVisualsComponent visuals) =>
        visuals.FormStart != null || visuals.ShatterStart != null;

    public static bool IsShatterFinished(ShipShieldVisualsComponent visuals, TimeSpan now) =>
        visuals.ShatterStart is { } start && now >= start + TimeSpan.FromSeconds(MathF.Max(visuals.ShatterTime, 0.01f));

    public static float GetShaderProgress(ShipShieldVisualsComponent visuals, TimeSpan now)
    {
        if (visuals.ShatterStart is { } shatterStart)
            return 1f + Fraction(now - shatterStart, visuals.ShatterTime);

        return visuals.FormStart is { } formStart ? Fraction(now - formStart, visuals.SpinupTime) : 0f;
    }

    private static float Fraction(TimeSpan elapsed, float duration) =>
        Math.Clamp((float) elapsed.TotalSeconds / MathF.Max(duration, 0.01f), 0f, 1f);
}
// LuaM-end
