using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.ADT.SS40k.Turrets.TurretControllable;

[RegisterComponent]
public sealed partial class TurretControllableComponent : Component
{
    [ViewVariables]
    public EntityUid? User;

    [ViewVariables]
    public EntityUid? Controller;// ??? why?

    [DataField("ControlReturnAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ControlReturnAction = "ControlReturnAction";
    [DataField("ControlReturnActionEntity")]
    public EntityUid? ControlReturnActEntity;
}
