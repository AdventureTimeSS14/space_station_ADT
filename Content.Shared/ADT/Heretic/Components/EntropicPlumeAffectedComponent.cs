//

using Content.Shared.ADT.Heretic.SpriteOverlay;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class EntropicPlumeAffectedComponent : BaseSpriteOverlayComponent
{
    [DataField, AutoNetworkedField]
    public EntityUid ExcludedEntity;

    [DataField]
    public float Duration = 10f;

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextAttack = TimeSpan.Zero;

    public override Enum Key { get; set; } = EntropicPlumeKey.Key;

    [DataField, AutoNetworkedField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("ADT/Heretic/Effects/effects.rsi"), "cloud_swirl");
}

public enum EntropicPlumeKey : byte
{
    Key,
}
