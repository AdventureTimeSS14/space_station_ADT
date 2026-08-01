using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Salvage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTCursedKatanaComponent : Component
{
    [DataField]
    public List<ADTKatanaCombo> Combos = new();

    [DataField]
    public bool DrewBlood;

    [DataField]
    public DamageSpecifier HungerDamage = new()
    {
        DamageDict = { ["Brute"] = 25 },
    };

    [DataField]
    public TimeSpan ComboWindow = TimeSpan.FromSeconds(5);

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
    public float DashRange = 1.5f;

    [DataField]
    public float DashDistance = 4f;

    [DataField]
    public float DashSpeed = 12f;

    [DataField]
    public DamageSpecifier HealCost = new()
    {
        DamageDict = { ["Slash"] = 15 },
    };

    [DataField]
    public DamageSpecifier HealAmount = new()
    {
        DamageDict = { ["Brute"] = -20 },
    };

    [DataField]
    public SoundSpecifier StrikeSound = new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg");

    [DataField]
    public SoundSpecifier CutSound = new SoundPathSpecifier("/Audio/Weapons/slash.ogg");

    [DataField]
    public SoundSpecifier DashSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    [DataField]
    public SoundSpecifier HealSound = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg");

    [DataField]
    public SoundSpecifier HungerSound = new SoundPathSpecifier("/Audio/Effects/demon_attack1.ogg");

    [ViewVariables]
    public readonly List<ADTKatanaInput> Inputs = new();

    [ViewVariables]
    public TimeSpan ResetAt;

    [ViewVariables]
    public EntityUid? CurrentTarget;

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
public sealed partial class ADTCursedKatanaBearerComponent : Component;

[DataDefinition]
public sealed partial class ADTKatanaCombo
{
    [DataField(required: true)]
    public List<ADTKatanaInput> Inputs = new();

    [DataField(required: true)]
    public ADTKatanaMove Move;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Popup;
}

public enum ADTKatanaInput : byte
{
    Light,
    Heavy,
}

public enum ADTKatanaMove : byte
{
    TendonCut,
    HiltStrike,
    Dash,
    DarkHeal,
}
