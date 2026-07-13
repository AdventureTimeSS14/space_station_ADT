using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Medical.IV;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BloodPackComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Solution = "pack";

    // TODO RMC-14 blood types
    [DataField, AutoNetworkedField]
    public string[] TransferableReagents = ["Blood"];
}

[Serializable, NetSerializable]
public enum BloodPackVisuals
{
    Label
}
