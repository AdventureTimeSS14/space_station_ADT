using Content.Shared.Anomaly.Effects;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.FixedPoint;

namespace Content.Shared.ADT.Implants;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MantisDaggersComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? InnateWeapon;

    [AutoNetworkedField]
    public MantisDaggerSide Sides = MantisDaggerSide.None;

    [DataField]
    public EntProtoId PairWeapon = "ADTMantisDaggers";

    [DataField]
    public EntProtoId SingleWeapon = "ADTMantisDagger";

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/ADT/Weapons/Melee/blade.ogg");

    [DataField]
    public SpriteSpecifier? LeftSprite = new SpriteSpecifier.Rsi(new("ADT/Implants/mantis_daggers.rsi"), "hands-left");

    [DataField]
    public SpriteSpecifier? RightSprite = new SpriteSpecifier.Rsi(new("ADT/Implants/mantis_daggers.rsi"), "hands-right");

    [DataField]
    public string LeftLayerMap = "mantis_daggers_layer_left";

    [DataField]
    public string RightLayerMap = "mantis_daggers_layer_right";

    [DataField]
    [AutoNetworkedField]
    public bool Active = false;

    [AutoNetworkedField]
    public EntityUid? ActionEntity;

    public Container Container = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EmpLastPulse = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan EmpCooldown = TimeSpan.FromSeconds(10f);

    [DataField]
    public DamageSpecifier EmpDamage = new()
    {
        DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
        {
            { "Shock", 20 },
        },
    };
}
