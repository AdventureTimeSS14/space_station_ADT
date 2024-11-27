using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.ADT.WW1;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AirStrikeComponent : Component
{
    public TimeSpan? FireTime;

    public bool IsArmed = false;

    public MapCoordinates StrikeOrigin { get; set; }

    public EntityCoordinates StrikeCoordinates { get; set; }

    public bool WarnSoundPlayed = false;

    [DataField, AutoNetworkedField]
    public TimeSpan FireDelay = TimeSpan.FromSeconds(15);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? WarnSound = new SoundPathSpecifier("/Audio/ADT/ADTGlobalEvents/WW1/Mortars/gun_mortar_travel.ogg");

    [DataField, AutoNetworkedField]
    public float WarnSoundDelayMultiplier = 2f;

    [DataField, AutoNetworkedField]
    public int ExplosionCount = 5;

    [DataField, AutoNetworkedField]
    public float MinOffset = 10f;

    [DataField, AutoNetworkedField]
    public float MaxOffset = 25f;

    [DataField, AutoNetworkedField]
    public float MinExplosionIntensity = 1f;

    [DataField, AutoNetworkedField]
    public float MaxExplosionIntensity = 3f;
}
