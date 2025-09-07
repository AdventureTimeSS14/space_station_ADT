using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Storage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SuitStorageComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<EntityPrototype>> Options = new();

    [DataField, AutoNetworkedField]
    public int? Selected;
}

[Serializable, NetSerializable]
public enum SuitStorageUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SuitStorageMessage(int index) : BoundUserInterfaceMessage
{
    public int Index = index;
}