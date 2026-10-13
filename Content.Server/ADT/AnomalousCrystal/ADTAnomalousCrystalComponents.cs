using Content.Shared.Maps;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.AnomalousCrystal;

public enum ADTCrystalActivation : byte
{
    Touch,
    Weapon,
    Bullet,
    Laser,
    Energy,
    Bomb,
    MobBump,
    Speech,
}

[RegisterComponent]
public sealed partial class ADTAnomalousCrystalComponent : Component
{
    [DataField]
    public List<ADTCrystalActivation> Methods = new()
    {
        ADTCrystalActivation.Touch,
        ADTCrystalActivation.Laser,
        ADTCrystalActivation.Bullet,
        ADTCrystalActivation.Energy,
        ADTCrystalActivation.Bomb,
        ADTCrystalActivation.MobBump,
        ADTCrystalActivation.Weapon,
        ADTCrystalActivation.Speech,
    };

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(3);

    [DataField]
    public SoundSpecifier ActivationSound = new SoundPathSpecifier("/Audio/Effects/break_stone.ogg");

    [ViewVariables]
    public ADTCrystalActivation Method;

    [ViewVariables]
    public TimeSpan NextUse;

    [ViewVariables]
    public HashSet<EntityUid> Affected = new();
}

[ByRefEvent]
public record struct ADTCrystalActivatedEvent(EntityUid? User, ADTCrystalActivation Method);

[RegisterComponent]
public sealed partial class ADTCrystalHonkComponent : Component
{
    [DataField]
    public ProtoId<StartingGearPrototype> Gear = "ClownGear";
}

[RegisterComponent]
public sealed partial class ADTCrystalThemeWarpComponent : Component
{
    [DataField]
    public List<ADTCrystalTheme> Themes = new();

    [DataField]
    public EntityWhitelist? ChairWhitelist;

    [DataField]
    public EntityWhitelist? TableWhitelist;

    [ViewVariables]
    public int Theme = -1;

    [ViewVariables]
    public HashSet<string> WarpedAreas = new();
}

[DataDefinition]
public sealed partial class ADTCrystalTheme
{
    [DataField]
    public ProtoId<ContentTileDefinition>? Floor;

    [DataField]
    public EntProtoId? Wall;

    [DataField]
    public EntProtoId? Chair;

    [DataField]
    public EntProtoId? Table;

    [DataField]
    public List<EntProtoId> Flora = new();

    [DataField]
    public float FloraChance = 0.08f;
}

[RegisterComponent]
public sealed partial class ADTCrystalEmitterComponent : Component
{
    [DataField]
    public List<EntProtoId> Projectiles = new();

    [DataField]
    public float Speed = 20f;

    [ViewVariables]
    public EntProtoId? Projectile;
}

[RegisterComponent]
public sealed partial class ADTCrystalDarkRepriseComponent : Component
{
    [DataField]
    public EntProtoId Mob = "ADTMobDarkRepriseShadow";

    [DataField]
    public float Range = 1.5f;

    [DataField]
    public EntProtoId Effect = "EffectSparks";
}

[RegisterComponent]
public sealed partial class ADTCrystalHelpersComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry RoleComponents = new();

    [ViewVariables]
    public bool Ready;
}

[RegisterComponent]
public sealed partial class ADTCrystalRefresherComponent : Component
{
    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public EntProtoId Effect = "EffectEmpPulse";
}

[RegisterComponent]
public sealed partial class ADTCrystalPossessorComponent : Component
{
    [DataField]
    public float Range = 1.5f;

    [DataField]
    public float MaxMeleeDamage = 5f;

    [DataField]
    public float MaxHealth = 100f;

    [DataField]
    public EntProtoId Fallback = "MobCockroach";

    [DataField]
    public EntProtoId ExitAction = "ActionADTExitPossession";
}

[RegisterComponent]
public sealed partial class ADTCrystalPossessedComponent : Component
{
    [ViewVariables]
    public EntityUid Body;

    [ViewVariables]
    public EntityUid? Action;

    [DataField]
    public string ContainerId = "adt_possession_stasis";
}
