using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.StormStaff;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTStormStaffComponent : Component
{
    [DataField]
    public TimeSpan StrikeDelay = TimeSpan.FromSeconds(1.5);

    [DataField]
    public int ShockDamage = 15;

    [DataField]
    public TimeSpan ShockStun = TimeSpan.FromSeconds(2);

    [DataField]
    public float CenterMultiplier = 2f;

    [DataField]
    public float StormMultiplier = 2f;

    [DataField]
    public float NonHumanoidMultiplier = 3f;

    [DataField]
    public DamageSpecifier ObjectDamage = new();

    [DataField]
    public int FireRange = 1;

    [DataField]
    public int StormFireRange = 2;

    [DataField]
    public float FireStacks = 1f;

    [DataField]
    public float StormExplosionIntensity = 4f;

    [DataField]
    public float StormExplosionSlope = 4f;

    [DataField]
    public float StormExplosionMaxTile = 2f;

    [DataField]
    public EntProtoId TargetEffect = "ADTEffectStormStaffTarget";

    [DataField]
    public EntProtoId BoltEffect = "ADTEffectThunderbolt";

    [DataField]
    public EntProtoId ShockEffect = "ADTEffectStormStaffElectricity";

    [DataField]
    public SoundSpecifier TargetSound = new SoundPathSpecifier("/Audio/Effects/Lightning/lightningshock.ogg");

    [DataField]
    public SoundSpecifier BoltSound = new SoundPathSpecifier("/Audio/Effects/Lightning/lightningbolt.ogg");

    [DataField]
    public SoundSpecifier DispelSound = new SoundPathSpecifier("/Audio/Magic/staff_change.ogg");

    [ViewVariables]
    public List<ADTStormStaffStrike> PendingStrikes = new();
}

public sealed class ADTStormStaffStrike
{
    public EntityUid Grid;
    public Vector2i Tile;
    public TimeSpan StrikeAt;
    public bool Boosted;
    public EntityUid? User;
}
