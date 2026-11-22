using Content.Shared.EntityEffects;
using Robust.Shared.Audio;

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

    [DataField]
    public List<ADTCrystalVariant> Variants = new();

    [DataField]
    public bool OncePerUser;

    [DataField]
    public int? MaxActivations;

    [ViewVariables]
    public ADTCrystalActivation Method;

    [ViewVariables]
    public ADTCrystalVariant? Variant;

    [ViewVariables]
    public TimeSpan NextUse;

    [ViewVariables]
    public int Activations;

    [ViewVariables]
    public HashSet<EntityUid> Affected = new();

    [ViewVariables]
    public HashSet<string> WarpedAreas = new();
}

[DataDefinition]
public sealed partial class ADTCrystalVariant
{
    [DataField]
    public EntityEffect[] Effects = Array.Empty<EntityEffect>();

    [DataField]
    public EntityEffect[] UserEffects = Array.Empty<EntityEffect>();
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
