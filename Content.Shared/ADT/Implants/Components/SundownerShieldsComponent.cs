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
public sealed partial class SundownerShieldsComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? InnateWeapon;

    [AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/ADT/Weapons/Melee/sundowner.ogg");

    /// <summary>
    /// The fallback sprite to be added on the original entity.
    /// </summary>
    [DataField]
    public SpriteSpecifier FallbackSprite = new SpriteSpecifier.Rsi(new("ADT/Implants/sundowner_shields.rsi"), "equipped-BELT");

    /// <summary>
    /// The fallback sprite to be added on the original entity.
    /// </summary>
    [DataField]
    public SpriteSpecifier FallbackSpriteClosed = new SpriteSpecifier.Rsi(new("ADT/Implants/sundowner_shields.rsi"), "equipped-BELT-closed");

    /// <summary>
    /// The key of the entity layer into which the sprite will be inserted
    /// </summary>
    [DataField]
    public string LayerMap = "sundowner_shields_layer";

    public Container Container = new();

    [AutoNetworkedField]
    public bool Active = false;

    public TimeSpan DisableTime = TimeSpan.Zero;
}
