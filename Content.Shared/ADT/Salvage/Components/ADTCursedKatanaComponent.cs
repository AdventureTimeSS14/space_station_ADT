using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Salvage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTCursedKatanaComponent : Component
{
    [DataField]
    public bool DrewBlood;

    [DataField]
    public DamageSpecifier HungerDamage = new()
    {
        DamageDict = { ["Brute"] = 25 },
    };

    [DataField]
    public DamageSpecifier CutDamage = new()
    {
        DamageDict = { ["Slash"] = 15 },
    };

    [DataField]
    public float CutBleed = 6f;

    [DataField]
    public DamageSpecifier StrikeDamage = new()
    {
        DamageDict = { ["Blunt"] = 17 },
    };

    [DataField]
    public DamageSpecifier StrikeImpactDamage = new()
    {
        DamageDict = { ["Blunt"] = 5 },
    };

    [DataField]
    public float StrikeThrowDistance = 5f;

    [DataField]
    public float StrikeThrowSpeed = 10f;

    [DataField]
    public TimeSpan StrikeStun = TimeSpan.FromSeconds(2);

    [DataField]
    public DamageSpecifier DashDamage = new()
    {
        DamageDict = { ["Slash"] = 12 },
    };

    [DataField]
    public DamageSpecifier DashSplashDamage = new()
    {
        DamageDict = { ["Slash"] = 5 },
    };

    [DataField]
    public DamageSpecifier DashTrailDamage = new()
    {
        DamageDict = { ["Slash"] = 15 },
    };

    [DataField]
    public float DashRange = 1.5f;

    [DataField]
    public int DashTiles = 3;

    [DataField]
    public float DashSpeed = 12f;

    [DataField]
    public DamageSpecifier HealCost = new()
    {
        DamageDict = { ["Slash"] = 15 },
    };

    [DataField]
    public LocId CutPopup = "adt-cursed-katana-perform-cut";

    [DataField]
    public LocId StrikePopup = "adt-cursed-katana-perform-strike";

    [DataField]
    public LocId DashPopup = "adt-cursed-katana-perform-dash";

    [DataField]
    public LocId HealPopup = "adt-cursed-katana-perform-heal";

    [DataField]
    public SoundSpecifier StrikeSound = new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg");

    [DataField]
    public SoundSpecifier CutSound = new SoundPathSpecifier("/Audio/Weapons/slash.ogg");

    [DataField]
    public SoundSpecifier DashSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    [DataField]
    public SoundSpecifier HealSound = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg");

    [DataField]
    public SoundSpecifier HungerSound = new SoundPathSpecifier("/Audio/Effects/demon_attack1.ogg");

    [DataField]
    public EntProtoId ImplantAction = "ADTActionToggleCursedKatana";

    [DataField]
    public SoundSpecifier ImplantSound = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg");

    [ViewVariables]
    public EntityUid? Holder;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTCursedKatanaShardComponent : Component
{
    [DataField]
    public EntProtoId Action = "ADTActionToggleCursedKatana";

    [DataField]
    public SoundSpecifier ConsumeSound = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg");

    [ViewVariables]
    public EntityUid? ActionEntity;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTCursedKatanaBearerComponent : Component
{
    [DataField]
    public EntProtoId Blade = "ADTKatanacursed";

    [DataField]
    public EntProtoId Remains = "Ash";

    [DataField]
    public SoundSpecifier ReleaseSound = new SoundPathSpecifier("/Audio/Effects/demon_attack1.ogg");
}

[RegisterComponent]
public sealed partial class ADTShadowMendComponent : Component
{
    [DataField]
    public FixedPoint2 HealPerTick = 15;

    [DataField]
    public List<ProtoId<DamageGroupPrototype>> HealGroups = new() { "Brute", "Burn" };

    [DataField]
    public TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(3);

    [DataField]
    public SoundSpecifier PriceSound = new SoundPathSpecifier("/Audio/Effects/demon_attack1.ogg");

    [ViewVariables]
    public TimeSpan NextTick;

    [ViewVariables]
    public TimeSpan EndTime;
}

[RegisterComponent]
public sealed partial class ADTVoidPriceComponent : Component
{
    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = { ["Blunt"] = 1 },
    };

    [DataField]
    public float Price = 3f;

    [DataField]
    public float PriceIncrease = 1f;

    [DataField]
    public TimeSpan TickInterval = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(30);

    [DataField]
    public SoundSpecifier TickSound = new SoundPathSpecifier("/Audio/Effects/bite.ogg");

    [ViewVariables]
    public TimeSpan NextTick;

    [ViewVariables]
    public TimeSpan EndTime;
}
