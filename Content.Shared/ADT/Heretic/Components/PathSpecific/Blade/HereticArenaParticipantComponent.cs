//

using Content.Shared.ADT.Heretic.SpriteOverlay;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Shared.Heretic.Components.PathSpecific.Blade;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class HereticArenaParticipantComponent : BaseSpriteOverlayComponent
{
    [DataField, AutoNetworkedField]
    public bool IsVictor;

    [DataField]
    public DamageModifierSet ModifierSet = new()
    {
        Coefficients =
        {
            { "Radiation", 0f },
        },
    };

    [DataField]
    public string FighterState = "arena_fighter";

    [DataField]
    public string VictorState = "arena_victor";

    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("ADT/Heretic/Effects/crown.rsi"), "arena_fighter");

    public override Vector2 Offset { get; set; } = new(0f, 2f / 3f);

    public override Enum Key { get; set; } = HereticArenaKey.Key;

    public override bool Unshaded { get; set; } = false;
}

public enum HereticArenaKey : byte
{
    Key,
}
