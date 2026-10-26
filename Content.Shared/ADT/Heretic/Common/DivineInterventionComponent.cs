using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Heretic.Common;

// ADT: from Goob Religion

[RegisterComponent, NetworkedComponent]
public sealed partial class DivineInterventionComponent : Component
{
    /// <summary>
    /// Which sound to play on spell denial.
    /// </summary>
    [DataField]
    public SoundSpecifier DenialSound = new SoundPathSpecifier("/Audio/Effects/hallelujah.ogg");

    /// <summary>
    /// Which effect to display.
    /// </summary>
    // ADT: prototype is named EffectSparks (plural)
    [DataField]
    public EntProtoId EffectProto = "EffectSparks";

    /// <summary>
    /// Which loc string to display.
    /// </summary>
    [DataField]
    public LocId DenialString = "nullrod-spelldenial-popup";

    /// <summary>
    /// Valid inventory slots for spell denial when equipped
    /// </summary>
    [DataField]
    public SlotFlags ValidSpellDenialSlots = SlotFlags.NONE;
}
