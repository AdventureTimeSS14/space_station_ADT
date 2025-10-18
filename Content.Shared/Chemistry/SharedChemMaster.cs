using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    /// <summary>
    /// This class holds constants that are shared between client and server.
    /// </summary>
    public sealed class SharedChemMaster
    {
        public const uint PillTypes = 20;
        public const string BufferSolutionName = "buffer";
        public const string InputSlotName = "beakerSlot";
        public const string OutputSlotName = "outputSlot";
        public const string PillSolutionName = "food";
        public const string BottleSolutionName = "drink";
        public const uint LabelMaxLength = 50;
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterSetModeMessage : BoundUserInterfaceMessage
    {
        public readonly ChemMasterMode ChemMasterMode;

        public ChemMasterSetModeMessage(ChemMasterMode mode)
        {
            ChemMasterMode = mode;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterSetPillTypeMessage : BoundUserInterfaceMessage
    {
        public readonly uint PillType;

        public ChemMasterSetPillTypeMessage(uint pillType)
        {
            PillType = pillType;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterReagentAmountButtonMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentId ReagentId;
        public readonly int Amount;
        public readonly bool FromBuffer;
        public readonly bool IsOutput;

        public ChemMasterReagentAmountButtonMessage(ReagentId reagentId, int amount, bool fromBuffer, bool isOutput)
        {
            ReagentId = reagentId;
            Amount = amount;
            FromBuffer = fromBuffer;
            IsOutput = isOutput;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterCreatePillsMessage : BoundUserInterfaceMessage
    {
        public readonly uint Dosage;
        public readonly uint Number;
        public readonly string Label;

        public ChemMasterCreatePillsMessage(uint dosage, uint number, string label)
        {
            Dosage = dosage;
            Number = number;
            Label = label;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterOutputToBottleMessage : BoundUserInterfaceMessage
    {
        public readonly uint Dosage;
        public readonly uint Number;
        public readonly string Label;

        public ChemMasterOutputToBottleMessage(uint dosage, uint number, string label)
        {
            Dosage = dosage;
            Number = number;
            Label = label;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterSortMethodUpdated(int sortMethod) : BoundUserInterfaceMessage
    {
        public readonly int SortMethod = sortMethod;
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterTransferringAmountUpdated(int transferringAmount) : BoundUserInterfaceMessage
    {
        public readonly int TransferringAmount = transferringAmount;
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterAmountsUpdated(List<int> amounts) : BoundUserInterfaceMessage
    {
        public readonly List<int> Amounts = amounts;
    }

    //ADT-Tweak Start: Bottle buttons reagent transfer
    [Serializable, NetSerializable]
    public sealed class ChemMasterSelectBottleSlotMessage : BoundUserInterfaceMessage
    {
        public readonly int Slot;

        public ChemMasterSelectBottleSlotMessage(int slot)
        {
            Slot = slot;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterChooseReagentMessage : BoundUserInterfaceMessage
    {
        public ReagentId Reagent;

        public ChemMasterChooseReagentMessage(ReagentId reagent)
        {
            Reagent = reagent;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterClearReagentSelectionMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterToggleBottleFillMessage : BoundUserInterfaceMessage
    {
        public int Slot;

        public ChemMasterToggleBottleFillMessage(int slot)
        {
            Slot = slot;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterRowEjectMessage : BoundUserInterfaceMessage
    {
        public readonly int Row;

        public ChemMasterRowEjectMessage(int row)
        {
            Row = row;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterReagentToggledOnMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentId Reagent;

        public ChemMasterReagentToggledOnMessage(ReagentId reagent)
        {
            Reagent = reagent;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterReagentToggledOffMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentId Reagent;

        public ChemMasterReagentToggledOffMessage(ReagentId reagent)
        {
            Reagent = reagent;
        }
    }

    // ADT-Tweak Start: Pill container messages
    [Serializable, NetSerializable]
    public sealed class ChemMasterSelectPillContainerSlotMessage : BoundUserInterfaceMessage
    {
        public readonly int Slot;

        public ChemMasterSelectPillContainerSlotMessage(int slot)
        {
            Slot = slot;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterTogglePillContainerFillMessage : BoundUserInterfaceMessage
    {
        public readonly int Slot;

        public ChemMasterTogglePillContainerFillMessage(int slot)
        {
            Slot = slot;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterPillContainerSlotEjectMessage : BoundUserInterfaceMessage
    {
        public readonly int Slot;

        public ChemMasterPillContainerSlotEjectMessage(int slot)
        {
            Slot = slot;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterPillContainerRowEjectMessage : BoundUserInterfaceMessage
    {
        public readonly int Row;

        public ChemMasterPillContainerRowEjectMessage(int row)
        {
            Row = row;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterSelectPillCanisterForCreationMessage : BoundUserInterfaceMessage
    {
        public readonly int CanisterIndex;

        public ChemMasterSelectPillCanisterForCreationMessage(int canisterIndex)
        {
            CanisterIndex = canisterIndex;
        }
    }

    // ADT-Tweak: Reagent amount selection messages
    [Serializable, NetSerializable]
    public sealed class ChemMasterSelectReagentAmountMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentId Reagent;
        public readonly int Amount;

        public ChemMasterSelectReagentAmountMessage(ReagentId reagent, int amount)
        {
            Reagent = reagent;
            Amount = amount;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterRemoveReagentAmountMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentId Reagent;
        public readonly int Amount;

        public ChemMasterRemoveReagentAmountMessage(ReagentId reagent, int amount)
        {
            Reagent = reagent;
            Amount = amount;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterClearReagentAmountMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentId Reagent;

        public ChemMasterClearReagentAmountMessage(ReagentId reagent)
        {
            Reagent = reagent;
        }
    }
    // ADT-Tweak End

    public enum ChemMasterMode
    {
        Transfer,
        Discard,
    }

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
    public sealed class ChemMasterBoundUserInterfaceState(
        ChemMasterMode mode,
        ContainerInfo? containerInfo,
        IReadOnlyList<ReagentQuantity> bufferReagents,
        FixedPoint2 bufferCurrentVolume,
        uint selectedPillType,
        uint pillDosageLimit,
        uint bottleDosageLimit,
        bool updateLabel,
        int sortMethod,
        int transferringAmount,
        List<int> amounts,
        //ADT-Tweak Start: Pill container storage
        List<ContainerInfo?> storedPillContainers,
        List<List<bool>> pillContainers,
        List<List<uint>> pillTypes, // ADT-Tweak: Pill types for each slot in each container
        int selectedPillContainerSlot,
        int selectedPillContainerForFill,
        int selectedPillCanisterForCreation,
        ReagentId? selectedReagent,
        //ADT-Tweak: Legacy bottle storage
        List<ContainerInfo?> storedBottles,
        int selectedBottleSlot,
        int selectedBottleForFill,
        List<ReagentId> selectedReagentsForBottles,
        Dictionary<ReagentId, int> selectedReagentAmounts)
        //ADT-Tweak End
        : BoundUserInterfaceState
    {
        public readonly ContainerInfo? ContainerInfo = containerInfo;

        /// <summary>
        /// A list of the reagents and their amounts within the buffer, if applicable.
        /// </summary>
        public readonly IReadOnlyList<ReagentQuantity> BufferReagents = bufferReagents;

        public readonly ChemMasterMode Mode = mode;

        public readonly FixedPoint2? BufferCurrentVolume = bufferCurrentVolume;
        public readonly uint SelectedPillType = selectedPillType;

        public readonly uint PillDosageLimit = pillDosageLimit;
        public readonly uint BottleDosageLimit = bottleDosageLimit;

        public readonly bool UpdateLabel = updateLabel;

        public readonly int SortMethod = sortMethod;
        public readonly int TransferringAmount = transferringAmount;

        public readonly List<int> Amounts = amounts;

        //ADT-Tweak Start: Pill container storage
        public readonly List<ContainerInfo?> StoredPillContainers = storedPillContainers;
        public readonly List<List<bool>> PillContainers = pillContainers;
        public readonly List<List<uint>> PillTypes = pillTypes; // ADT-Tweak: Pill types for each slot in each container
        public readonly int SelectedPillContainerSlot = selectedPillContainerSlot;
        public readonly int SelectedPillContainerForFill = selectedPillContainerForFill;
        public readonly int SelectedPillCanisterForCreation = selectedPillCanisterForCreation;
        public readonly ReagentId? SelectedReagent = selectedReagent;
        //ADT-Tweak: Legacy bottle storage
        public readonly List<ContainerInfo?> StoredBottles = storedBottles;
        public readonly int SelectedBottleSlot = selectedBottleSlot;
        public readonly int SelectedBottleForFill = selectedBottleForFill;
        public readonly List<ReagentId> SelectedReagentsForBottles = selectedReagentsForBottles;
        public readonly Dictionary<ReagentId, int> SelectedReagentAmounts = selectedReagentAmounts;
        //ADT-Tweak End
    }

    [Serializable, NetSerializable]
    public enum ChemMasterUiKey
    {
        Key
    }
}
