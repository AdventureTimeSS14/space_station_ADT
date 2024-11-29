﻿using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Mortar;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedMortarSystem))]
public sealed partial class MortarComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "rmc_mortar_container";

    [DataField, AutoNetworkedField]
    public TimeSpan DeployDelay = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public TimeSpan TargetDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan DialDelay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public bool Deployed;

    [DataField, AutoNetworkedField]
    public Vector2i Target;

    [DataField, AutoNetworkedField]
    public Vector2i Offset;

    [DataField, AutoNetworkedField]
    public Vector2i Dial;

    [DataField, AutoNetworkedField]
    public TimeSpan FireDelay = TimeSpan.FromSeconds(9);

    [DataField, AutoNetworkedField]
    public int TilesPerOffset = 20;

    [DataField, AutoNetworkedField]
    public int MaxTarget = 1000;

    [DataField, AutoNetworkedField]
    public int MaxDial = 10;

    [DataField, AutoNetworkedField]
    public int MinimumRange = 10;

    [DataField, AutoNetworkedField]
    public string FixtureId = "mortar";

    [DataField, AutoNetworkedField]
    public TimeSpan AnimationTime = TimeSpan.FromSeconds(0.3);

    [DataField, AutoNetworkedField]
    public string AnimationLayer = "mortar";

    [DataField, AutoNetworkedField]
    public string AnimationState = "mortar_m402_fire";

    [DataField, AutoNetworkedField]
    public string DeployedState = "mortar_m402";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DeploySound = new SoundPathSpecifier("/Audio/ADT/ADTGlobalEvents/WW1/Mortars/gun_mortar_unpack.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ReloadSound = new SoundPathSpecifier("/Audio/ADT/ADTGlobalEvents/WW1/Mortars/gun_mortar_reload.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? FireSound = new SoundPathSpecifier("/Audio/ADT/ADTGlobalEvents/WW1/Mortars/gun_mortar_fire.ogg");

    [DataField, AutoNetworkedField]
    public TimeSpan? Cooldown;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastFiredAt;
}
