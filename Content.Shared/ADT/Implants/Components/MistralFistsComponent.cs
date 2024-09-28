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
public sealed partial class MistralFistsComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? InnateWeapon;

    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// The fallback sprite to be added on the original entity.
    /// </summary>
    [DataField]
    public SpriteSpecifier? FallbackSprite = new SpriteSpecifier.Rsi(new("ADT/Implants/mistral_fists.rsi"), "equipped-HANDS");

    /// <summary>
    /// The key of the entity layer into which the sprite will be inserted
    /// </summary>
    [DataField]
    public string LayerMap = "mistral_fists_layer";

    public Container Container = new();
}
