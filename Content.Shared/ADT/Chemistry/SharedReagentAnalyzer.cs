using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;
using Content.Shared.Chemistry;

namespace Content.Shared.ADT.Chemistry
{
    /// <summary>
    /// This class holds constants that are shared between client and server.
    /// </summary>
    public sealed class SharedReagentAnalyzer
    {
        public const string OutputSlotName = "beakerSlot";
    }


    [Serializable, NetSerializable]
    public sealed class ReagentAnalyzerReagentMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentId ReagentId;

        public ReagentAnalyzerReagentMessage(ReagentId reagentId)
        {
            ReagentId = reagentId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ReagentAnalyzerClearContainerSolutionMessage : BoundUserInterfaceMessage
    {

    }

    [Serializable, NetSerializable]
    public sealed class ReagentAnalyzerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly ContainerInfo? OutputContainer;
        public ReagentAnalyzerBoundUserInterfaceState(ContainerInfo? outputContainer)
        {
            OutputContainer = outputContainer;
        }
    }

    [Serializable, NetSerializable]
    public enum ReagentAnalyzerUiKey
    {
        Key
    }
}
