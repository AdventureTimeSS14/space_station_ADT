using Content.Shared.Anomaly.Effects;
using Content.Shared.Body.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Implants;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MantisDaggersComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? InnateWeapon;

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/ADT/Weapons/Melee/blade.ogg");

    /// <summary>
    /// The fallback sprite to be added on the original entity.
    /// </summary>
    [DataField]
    public SpriteSpecifier? FallbackSprite = new SpriteSpecifier.Rsi(new("ADT/Implants/mantis_daggers.rsi"), "hands");

    /// <summary>
    /// The key of the entity layer into which the sprite will be inserted
    /// </summary>
    [DataField]
    public string LayerMap = "mantis_daggers_layer";

    [DataField]
    [AutoNetworkedField]
    public bool Active = false;

    [AutoNetworkedField]
    public EntityUid? ActionEntity;

    public Container Container = new();
}
