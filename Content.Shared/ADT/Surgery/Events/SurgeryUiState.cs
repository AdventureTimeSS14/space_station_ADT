using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Events;

[Serializable, NetSerializable]
public sealed class SurgeryChooseGraphState : BoundUserInterfaceState
{
    public List<(string GraphId, string Name, string Category, string Subcategory)> Options;

    public SurgeryChooseGraphState(List<(string, string, string, string)> options)
    {
        Options = options;
    }
}

[Serializable, NetSerializable]
public sealed class SurgeryChooseEdgeState : BoundUserInterfaceState
{
    public List<(string EdgeId, string Label)> Options;

    public SurgeryChooseEdgeState(List<(string, string)> options)
    {
        Options = options;
    }
}

[Serializable, NetSerializable]
public sealed class SurgerySelectGraphMessage : BoundUserInterfaceMessage
{
    public string GraphId;

    public SurgerySelectGraphMessage(string graphId)
    {
        GraphId = graphId;
    }
}

[Serializable, NetSerializable]
public sealed class SurgerySelectEdgeMessage : BoundUserInterfaceMessage
{
    public string EdgeId;

    public SurgerySelectEdgeMessage(string edgeId)
    {
        EdgeId = edgeId;
    }
}
