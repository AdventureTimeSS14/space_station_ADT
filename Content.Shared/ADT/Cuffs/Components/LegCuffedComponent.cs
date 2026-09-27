using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Cuffs.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class LegCuffedComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier CuffedSprite =
        new SpriteSpecifier.Rsi(new ResPath("ADT/Objects/Misc/legcuffs.rsi"), "leg-irons");

    [DataField]
    public TimeSpan BreakoutSoundCooldown = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan NextAllowedTime;
}
