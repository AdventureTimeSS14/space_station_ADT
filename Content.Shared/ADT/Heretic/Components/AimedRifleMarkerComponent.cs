//

using Content.Shared.ADT.Heretic.SpriteOverlay;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AimedRifleMarkerComponent : BaseSpriteOverlayComponent
{
    public override Enum Key { get; set; } = LionhunterAimMarkerKey.Key;

    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("ADT/Heretic/Effects/effects.rsi"), "sniper_zoom");
}

public enum LionhunterAimMarkerKey : byte
{
    Key,
}
