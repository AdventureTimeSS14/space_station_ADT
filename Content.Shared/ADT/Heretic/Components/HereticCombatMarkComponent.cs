//

using Content.Shared.ADT.Heretic.SpriteOverlay;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Heretic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class HereticCombatMarkComponent : BaseSpriteOverlayComponent
{
    [DataField, AutoNetworkedField]
    public string Path = "Blade";

    [DataField]
    public float MaxDisappearTime = 15f;

    [DataField]
    public float DisappearTime = 15f;

    [DataField]
    public int Repetitions = 1;

    public TimeSpan Timer = TimeSpan.Zero;

    [DataField]
    public SoundSpecifier? TriggerSound = new SoundPathSpecifier("/Audio/ADT/Heretic/repulse.ogg");

    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new("ADT/Heretic/combat_marks.rsi"), "blade");

    public override Enum Key { get; set; } = HereticCombatMarkKey.Key;
}

public enum HereticCombatMarkKey : byte
{
    Key,
}
