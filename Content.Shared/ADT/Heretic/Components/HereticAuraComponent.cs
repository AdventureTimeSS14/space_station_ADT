using Content.Shared.ADT.Heretic.SpriteOverlay;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HereticAuraComponent : BaseSpriteOverlayComponent
{
    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("ADT/Heretic/Effects/effects.rsi"), "heretic_aura");

    public override Enum Key { get; set; } = HereticAuraKey.Key;
}

public enum HereticAuraKey : byte
{
    Key,
}
