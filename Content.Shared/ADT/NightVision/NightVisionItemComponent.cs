using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.NightVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedNightVisionSystem))]
public sealed partial class NightVisionItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "ActionToggleNightVision";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [AutoNetworkedField]
    public EntityUid? User;

    [DataField, AutoNetworkedField]
    public bool Toggleable = true;

    // Only allows for a single slotflag right now because some code uses strings and some code uses enums to determine slots :(
    [DataField, AutoNetworkedField]
    public SlotFlags SlotFlags { get; set; } = SlotFlags.EYES;

    /// <summary>
    /// Screen shader used while this device is active (green NVD look).
    /// </summary>
    [DataField]
    public string Shader = "ADTNightVisionDeviceShader";

    /// <summary>
    /// PointLight effect while this device is active.
    /// </summary>
    [DataField]
    public EntProtoId EffectPrototype = "ADTEffectNightVisionDevice";

    /// <summary>
    /// Previous wearer shader restored when unequipping over innate NV.
    /// </summary>
    [ViewVariables]
    public string? PreviousShader;

    /// <summary>
    /// Previous wearer effect restored when unequipping over innate NV.
    /// </summary>
    [ViewVariables]
    public EntProtoId? PreviousEffectPrototype;
}
