using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Changes footstep sound
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FootstepModifierComponent : Component, IClothingSlots // ADT-Tweak
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier? FootstepSoundCollection;

    // ADT-Tweak-Start
    [DataField, AutoNetworkedField]
    public SlotFlags Slots { get; set; } = SlotFlags.FEET;
    // ADT-Tweak-End
}
