using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Weapons.Ranged.Components;

/// <summary>
/// Every shot heats the gun up through the temperature system, and the air around it cools it back down.
/// A hot gun starts to jam, and an overheated one melts its firing pin.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class GunHeatComponent : Component
{
    /// <summary>
    /// Heat in joules a single shot adds to the gun. The temperature rise depends on the gun's heat capacity.
    /// </summary>
    [DataField]
    public float HeatPerShot = 750f;

    /// <summary>
    /// Multiplier on <see cref="HeatPerShot"/> and <see cref="TemperaturePerShot"/>.
    /// 2 keeps firearms at twice the prototype shot. Lasers set their own rate.
    /// </summary>
    [DataField]
    public float HeatRate = 2f;

    /// <summary>
    /// Multiplier on how fast the gun sheds heat. 1 uses <see cref="PassiveCooling"/> or <see cref="CoolingTime"/> as written.
    /// </summary>
    [DataField]
    public float CoolRate = 1f;

    /// <summary>
    /// How far the hot color reaches back from the muzzle, in tiles, when the glow just starts and when it is full.
    /// </summary>
    [DataField]
    public float GlowReachStart = 0.015f;

    [DataField]
    public float GlowReachFull = 0.12f;

    /// <summary>
    /// How far the glow fades out from the muzzle, in tiles, at the start of the glow and at full heat.
    /// </summary>
    [DataField]
    public float GlowFadeStart = 0.10f;

    [DataField]
    public float GlowFadeFull = 0.55f;

    /// <summary>
    /// Minimum gap between the hot color and the end of the fade, in tiles.
    /// </summary>
    [DataField]
    public float GlowFadeGap = 0.04f;

    /// <summary>
    /// In-hand glow length in pixels when the glow just starts and when it is full.
    /// </summary>
    [DataField]
    public float InhandReachStart = 1.5f;

    [DataField]
    public float InhandReachFull = 9f;

    /// <summary>
    /// Kelvin added by a single shot. When set, this is the temperature rise regardless of the gun's mass.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TemperaturePerShot;

    /// <summary>
    /// Chance to jam on every shot, before heat is considered. 0.0005 is about one jam per 2000 shots.
    /// </summary>
    [DataField]
    public float BaseJamChance;

    /// <summary>
    /// Seconds of Newton's-law cooling. Zero keeps the watt-based <see cref="PassiveCooling"/> model.
    /// </summary>
    [DataField]
    public float CoolingTime;

    /// <summary>
    /// The gas system burns out at this temperature and the gun will not cycle again. Zero disables it.
    /// </summary>
    [DataField]
    public float FailureTemperature;

    /// <summary>
    /// Bare hands cannot pick the gun up at or above this temperature. Zero disables the check.
    /// </summary>
    [DataField]
    public float UnsafePickupTemperature;

    /// <summary>
    /// The gas system has already burned out.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Failed;

    /// <summary>
    /// The temperature of the surrounding air that the gun is in. The gun will cool down to this temperature over time.
    /// </summary>
    [DataField]
    public float EnvironmentHeatMargin = 20f;

    /// <summary>
    /// How well the surrounding air cools the gun.
    /// </summary>
    [DataField]
    public float CoolingEfficiency = 0.05f;

    /// <summary>
    /// Heat in watts per kelvin above room temperature that the gun loses on its own, even in vacuum.
    /// </summary>
    [DataField]
    public float PassiveCooling = 1.25f;

    /// <summary>
    /// Minimum temperature at which the gun starts to glow red-hot.
    /// The yellow mark on the heat gauge sits here.
    /// </summary>
    [DataField]
    public float GlowTemperature = Atmospherics.T20C + 250f;

    /// <summary>
    /// Where the muzzle is across the world sprite: 0 is the left edge, 1 is the right.
    /// Sheets with empty pixels past the barrel set this on the last painted pixel.
    /// </summary>
    [DataField]
    public float MuzzleFraction = 1f;

    /// <summary>
    /// The gun may jam from this temperature.
    /// </summary>
    [DataField]
    public float JamTemperature = Atmospherics.T20C + 160f;

    /// <summary>
    /// Chance to jam per shot once the gun reaches <see cref="MeltTemperature"/>; it grows linearly from <see cref="JamTemperature"/>.
    /// </summary>
    [DataField]
    public float MaxJamChance = 0.35f;

    /// <summary>
    /// The firing pin melts at this temperature.
    /// </summary>
    [DataField]
    public float MeltTemperature = Atmospherics.T20C + 360f;

    /// <summary>
    /// The entity prototype of the melted firing pin, used when the pin does not set its own
    /// <see cref="FiringPins.FiringPinComponent.MeltedPrototype"/>.
    /// </summary>
    [DataField]
    public EntProtoId? MeltedPin = "FiringPinMelted";

    /// <summary>
    /// Current gun temperature.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Temperature = Atmospherics.T20C;

    /// <summary>
    /// Whether the gun is currently jammed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Jammed;

    /// <summary>
    /// The sound that plays when the gun jams.
    /// </summary>
    [DataField]
    public SoundSpecifier JamSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Empty/empty.ogg");

    /// <summary>
    /// The sound that plays when the gun is unjammed.
    /// </summary>
    [DataField]
    public SoundSpecifier UnjamSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/smg_cock.ogg");

    /// <summary>
    /// The sound that plays when the gun melts.
    /// </summary>
    [DataField]
    public SoundSpecifier MeltSound = new SoundPathSpecifier("/Audio/Effects/sizzle.ogg");

    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(1);
    public TimeSpan NextPopupTime;
}

