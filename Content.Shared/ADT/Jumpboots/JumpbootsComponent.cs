using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Map;
using Robust.Shared.Serialization;


namespace Content.Shared.Clothing;

[RegisterComponent, NetworkedComponent(), AutoGenerateComponentState]
[Access(typeof(SharedMagbootsSystem))]
public sealed partial class JumpbootsComponent : Component
{
    /// <summary>
    /// Volume control for the spell.
    /// </summary>
    [DataField("jumpVolume")]
    public float Volume = 1f;

    [DataField("jumpStrength")]
    public float Strength = 13f;

    public SlotFlags AllowedSlots = SlotFlags.FEET;

    [DataField("jumpAction")]
    public EntProtoId Action = "ActionJumpboots";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}