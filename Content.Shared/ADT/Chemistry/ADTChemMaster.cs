using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Chemistry
{
    /// <summary>
    /// This class holds constants that are shared between client and server.
    /// </summary>
    public sealed class ADTADTChemMaster
    {
        public const uint PillTypes = 20;
        public const string BufferSolutionName = "buffer";
        public const string InputSlotName = "beakerSlot";
        public const string OutputSlotName = "outputSlot";
        public const string PillSolutionName = "food";
        public const string BottleSolutionName = "drink";
        public const uint LabelMaxLength = 150; // ADT-Tweak: Increased to support more reagents
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterSetModeMessage : BoundUserInterfaceMessage
    {
        public readonly ADTChemMasterMode ADTChemMasterMode;

        public ADTChemMasterSetModeMessage(ADTChemMasterMode mode)
        {
            ADTChemMasterMode = mode;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterSetPillTypeMessage : BoundUserInterfaceMessage
    {
        public readonly uint PillType;

        public ADTChemMasterSetPillTypeMessage(uint pillType)
        {
            PillType = pillType;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterReagentAmountButtonMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentId ReagentId;
        public readonly int Amount;
        public readonly bool FromBuffer;
        public readonly bool IsOutput;

        public ADTChemMasterReagentAmountButtonMessage(ReagentId reagentId, int amount, bool fromBuffer, bool isOutput)
        {
            ReagentId = reagentId;
            Amount = amount;
            FromBuffer = fromBuffer;
            IsOutput = isOutput;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterCreatePillsMessage : BoundUserInterfaceMessage
    {
        public readonly uint Dosage;
        public readonly uint Number;
        public readonly string Label;

        public ADTChemMasterCreatePillsMessage(uint dosage, uint number, string label)
        {
            Dosage = dosage;
            Number = number;
            Label = label;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterOutputToBottleMessage : BoundUserInterfaceMessage
    {
        public readonly uint Dosage;
        public readonly uint Number;
        public readonly string Label;

        public ADTChemMasterOutputToBottleMessage(uint dosage, uint number, string label)
        {
            Dosage = dosage;
            Number = number;
            Label = label;
        }
    }

    // ADT-Tweak Start: Messages for Handlers
    [Serializable, NetSerializable]
    public sealed class ADTChemMasterSortMethodUpdated(int sortMethod) : BoundUserInterfaceMessage
    {
        public readonly int SortMethod = sortMethod;
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterTransferringAmountUpdated(int transferringAmount) : BoundUserInterfaceMessage
    {
        public readonly int TransferringAmount = transferringAmount;
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterAmountsUpdated(List<int> amounts) : BoundUserInterfaceMessage
    {
        public readonly List<int> Amounts = amounts;
    }

    //Bottle buttons reagent transfer
    [Serializable, NetSerializable]
    public sealed class ADTChemMasterChooseReagentMessage : BoundUserInterfaceMessage
    {
        public ReagentId Reagent;

        public ADTChemMasterChooseReagentMessage(ReagentId reagent)
        {
            Reagent = reagent;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterClearReagentSelectionMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterToggleBottleFillMessage : BoundUserInterfaceMessage
    {
        public int Slot;

        public ADTChemMasterToggleBottleFillMessage(int slot)
        {
            Slot = slot;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterRowEjectMessage : BoundUserInterfaceMessage
    {
        public readonly int Row;

        public ADTChemMasterRowEjectMessage(int row)
        {
            Row = row;
        }
    }

    // Pill container messages
    [Serializable, NetSerializable]
    public sealed class ADTChemMasterSelectPillContainerSlotMessage : BoundUserInterfaceMessage
    {
        public readonly int Slot;

        public ADTChemMasterSelectPillContainerSlotMessage(int slot)
        {
            Slot = slot;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterTogglePillContainerFillMessage : BoundUserInterfaceMessage
    {
        public readonly int Slot;

        public ADTChemMasterTogglePillContainerFillMessage(int slot)
        {
            Slot = slot;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterPillContainerSlotEjectMessage : BoundUserInterfaceMessage
    {
        public readonly int Slot;

        public ADTChemMasterPillContainerSlotEjectMessage(int slot)
        {
            Slot = slot;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterPillContainerRowEjectMessage : BoundUserInterfaceMessage
    {
        public readonly int Row;

        public ADTChemMasterPillContainerRowEjectMessage(int row)
        {
            Row = row;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterSelectPillCanisterForCreationMessage : BoundUserInterfaceMessage
    {
        public readonly int CanisterIndex;

        public ADTChemMasterSelectPillCanisterForCreationMessage(int canisterIndex)
        {
            CanisterIndex = canisterIndex;
        }
    }

    // Reagent amount selection messages
    [Serializable, NetSerializable]
    public sealed class ADTChemMasterSelectReagentAmountMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentId Reagent;
        public readonly int Amount;

        public ADTChemMasterSelectReagentAmountMessage(ReagentId reagent, int amount)
        {
            Reagent = reagent;
            Amount = amount;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterRemoveReagentAmountMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentId Reagent;
        public readonly int Amount;

        public ADTChemMasterRemoveReagentAmountMessage(ReagentId reagent, int amount)
        {
            Reagent = reagent;
            Amount = amount;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterClearReagentAmountMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentId Reagent;

        public ADTChemMasterClearReagentAmountMessage(ReagentId reagent)
        {
            Reagent = reagent;
        }
    }

    public enum ADTChemMasterMode
    {
        Transfer,
        Discard,
    }
    // ADT-Tweak End

    /// <summary>
    /// Information about the capacity and contents of a container for display in the UI
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ContainerInfo
    {
        /// <summary>
        /// The container name to show to the player
        /// </summary>
        public readonly string DisplayName;

        /// <summary>
        /// The currently used volume of the container
        /// </summary>
        public readonly FixedPoint2 CurrentVolume;

        /// <summary>
        /// The maximum volume of the container
        /// </summary>
        public readonly FixedPoint2 MaxVolume;

        /// <summary>
        /// A list of the entities and their sizes within the container
        /// </summary>
        public List<(string Id, FixedPoint2 Quantity)>? Entities { get; init; }

        public List<ReagentQuantity>? Reagents { get; init; }

        public ContainerInfo(string displayName, FixedPoint2 currentVolume, FixedPoint2 maxVolume)
        {
            DisplayName = displayName;
            CurrentVolume = currentVolume;
            MaxVolume = maxVolume;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ADTChemMasterBoundUserInterfaceState(
        // ADT-Tweak Start: state container
        ADTChemMasterMode mode,
        ContainerInfo? containerInfo,
        IReadOnlyList<ReagentQuantity> bufferReagents,
        FixedPoint2 bufferCurrentVolume,
        uint selectedPillType,
        uint pillDosageLimit,
        uint bottleDosageLimit,
        uint maxPills,
        uint maxBottles,
        bool updateLabel,
        int sortMethod,
        int transferringAmount,
        List<int> amounts,
        // Pill container storage
        List<ContainerInfo?> storedPillContainers,
        List<List<bool>> pillContainers,
        List<List<uint>> pillTypes,
        int selectedPillContainerSlot,
        int selectedPillContainerForFill,
        int selectedPillCanisterForCreation,
        ReagentId? selectedReagent,
        ContainerInfo? selectedPillContainerInfo,
        // Bottle container storage
        List<ContainerInfo?> storedBottles,
        int selectedBottleForFill,
        List<ReagentId> selectedReagentsForBottles,
        Dictionary<ReagentId, float> selectedReagentAmounts)
        // ADT-Tweak End
        : BoundUserInterfaceState
    {
        // ADT-Tweak Start: A list of the reagents and their amounts within the buffer, if applicable.
        public readonly ContainerInfo? ContainerInfo = containerInfo;
        public readonly IReadOnlyList<ReagentQuantity> BufferReagents = bufferReagents;

        public readonly ADTChemMasterMode Mode = mode;

        public readonly FixedPoint2? BufferCurrentVolume = bufferCurrentVolume;
        public readonly uint SelectedPillType = selectedPillType;

        public readonly uint PillDosageLimit = pillDosageLimit;
        public readonly uint BottleDosageLimit = bottleDosageLimit;
        public readonly uint MaxPills = maxPills;
        public readonly uint MaxBottles = maxBottles;

        public readonly bool UpdateLabel = updateLabel;

        public readonly int SortMethod = sortMethod;
        public readonly int TransferringAmount = transferringAmount;

        public readonly List<int> Amounts = amounts;

        // Pill container storage
        public readonly List<ContainerInfo?> StoredPillContainers = storedPillContainers;
        public readonly List<List<bool>> PillContainers = pillContainers;
        public readonly List<List<uint>> PillTypes = pillTypes;
        public readonly int SelectedPillContainerSlot = selectedPillContainerSlot;
        public readonly int SelectedPillContainerForFill = selectedPillContainerForFill;
        public readonly int SelectedPillCanisterForCreation = selectedPillCanisterForCreation;
        public readonly ReagentId? SelectedReagent = selectedReagent;
        public readonly ContainerInfo? SelectedPillContainerInfo = selectedPillContainerInfo;
        // Bottle container storage
        public readonly List<ContainerInfo?> StoredBottles = storedBottles;
        public readonly int SelectedBottleForFill = selectedBottleForFill;
        public readonly List<ReagentId> SelectedReagentsForBottles = selectedReagentsForBottles;
        public readonly Dictionary<ReagentId, float> SelectedReagentAmounts = selectedReagentAmounts;
        // ADT-Tweak End
    }

    [Serializable, NetSerializable]
    public enum ADTChemMasterUiKey
    {
        Key
    }
}
