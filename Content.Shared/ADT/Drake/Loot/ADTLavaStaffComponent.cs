using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Drake.Loot;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTLavaStaffComponent : Component
{
    [DataField]
    public EntProtoId LavaProto = "FloorLavaEntity";

    [DataField]
    public List<EntProtoId> LavaPrototypes = new() { "FloorLavaEntity", "ADTFloorLavaCovered", "ADTDrakeTempLava" };

    [DataField]
    public string ResetTile = "FloorBasalt";

    [DataField]
    public EntProtoId WarningProto = "ADTLavaStaffWarning";

    [DataField]
    public EntProtoId FailEffect = "EffectSparks";

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public float Range = 8f;

    [DataField]
    public TimeSpan CreateDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan CreateCooldown = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan ResetCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan FailCooldown = TimeSpan.FromSeconds(3.1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUse;

    [DataField]
    public SoundSpecifier? UseSound = new SoundPathSpecifier("/Audio/Magic/fireball.ogg");

    [DataField]
    public SoundSpecifier? FailSound = new SoundCollectionSpecifier("sparks");
}

[Serializable, NetSerializable]
public sealed partial class ADTLavaStaffDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public NetCoordinates Location;

    [DataField]
    public NetEntity? Warning;

    private ADTLavaStaffDoAfterEvent()
    {
    }

    public ADTLavaStaffDoAfterEvent(NetCoordinates location, NetEntity? warning)
    {
        Location = location;
        Warning = warning;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
