using Content.Shared.Inventory;

namespace Content.Shared.Radio.Components;

/// <summary>
///     This component relays radio messages to the parent entity's chat when equipped.
/// </summary>
[RegisterComponent]
public sealed partial class HeadsetComponent : Component
{
    [DataField("enabled")]
    public bool Enabled = true;

    public bool IsEquipped = false;

    [DataField("requiredSlot")]
    public SlotFlags RequiredSlot = SlotFlags.EARS;

    /// <summary>
    ///     Ganimed edit
    ///     Determines how much larger the radio message font size will be.
    ///     Only applied if RadioBoostEnabled is true.
    /// </summary>
    [DataField]
    public int? RadioTextIncrease { get; set; } = 0;

    /// <summary>
    ///     Whether radio font size boost is currently active.
    /// </summary>
    [DataField]
    public bool RadioBoostEnabled = false;
    /// Ganimed edit end
}

