using Content.Shared.EntityEffects;
using Content.Shared.Polymorph;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.AnomalousCrystal;

public sealed partial class ADTCrystalThemeWarp : EntityEffectBase<ADTCrystalThemeWarp>
{
    [DataField(required: true)]
    public ProtoId<ADTCrystalThemePrototype> Theme;

    [DataField]
    public EntityWhitelist? ChairWhitelist;

    [DataField]
    public EntityWhitelist? TableWhitelist;
}

public sealed partial class ADTCrystalEmitProjectile : EntityEffectBase<ADTCrystalEmitProjectile>
{
    [DataField(required: true)]
    public EntProtoId Projectile;

    [DataField]
    public float Speed = 20f;
}

public sealed partial class ADTCrystalDarkReprise : EntityEffectBase<ADTCrystalDarkReprise>
{
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Polymorph;

    [DataField]
    public float Range = 1.5f;

    [DataField]
    public EntProtoId? Effect;

    [DataField]
    public List<string> RemoveLanguages = new();

    [DataField]
    public LocId? Popup;
}

public sealed partial class ADTAddComponents : EntityEffectBase<ADTAddComponents>
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    [DataField]
    public LocId? Popup;
}

public sealed partial class ADTCrystalRefreshItem : EntityEffectBase<ADTCrystalRefreshItem>
{
    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public EntProtoId? Effect;
}

public sealed partial class ADTCrystalPossess : EntityEffectBase<ADTCrystalPossess>
{
    [DataField]
    public EntityWhitelist? UserWhitelist;

    [DataField]
    public float Range = 1.5f;

    [DataField]
    public float MaxMeleeDamage = 5f;

    [DataField]
    public float MaxHealth = 100f;

    [DataField]
    public EntProtoId? Fallback;

    [DataField(required: true)]
    public EntProtoId ExitAction;
}

public sealed partial class ADTEquipStartingGear : EntityEffectBase<ADTEquipStartingGear>
{
    [DataField(required: true)]
    public ProtoId<StartingGearPrototype> Gear;
}
