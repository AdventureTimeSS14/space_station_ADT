using Robust.Shared.Prototypes;

namespace Content.Server._TornadoTech.FriendlyFire.Actions;

[RegisterComponent]
public sealed partial class FriendlyFireToggleableComponent : Component
{
    [DataField]
    public EntProtoId Prototype = "ActionToggleFriendlyFire";

    [DataField]
    public EntityUid? Action;
}
