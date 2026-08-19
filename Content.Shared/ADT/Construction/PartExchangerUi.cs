using Content.Shared.ADT.Construction.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Construction;

[Serializable, NetSerializable]
public enum PartExchangerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class PartExchangerFilterState : BoundUserInterfaceState
{
    public List<ProtoId<MachinePartPrototype>> AvailableParts = new();
    public List<ProtoId<MachinePartPrototype>> SelectedParts = new();
}

[Serializable, NetSerializable]
public sealed class PartExchangerFilterChangedMessage : BoundUserInterfaceMessage
{
    public List<ProtoId<MachinePartPrototype>> SelectedParts = new();
}
