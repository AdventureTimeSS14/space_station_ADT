using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// An industrial grade chemical manipulator with pill and bottle production included.
    /// <seealso cref="ChemMasterSystem"/>
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ChemMasterSystem))]
    public sealed partial class ChemMasterComponent : Component
    {
        [DataField("pillType"), ViewVariables(VVAccess.ReadWrite)]
        public uint PillType = 0;

        [DataField("mode"), ViewVariables(VVAccess.ReadWrite)]
        public ChemMasterMode Mode = ChemMasterMode.Transfer;

        [DataField("pillDosageLimit", required: true), ViewVariables(VVAccess.ReadWrite)]
        public uint PillDosageLimit;

        // ADT-Tweak-Start: лимит дозировки для бутылочек
        [DataField("bottleDosageLimit", required: true), ViewVariables(VVAccess.ReadWrite)]
        public uint BottleDosageLimit;
        // ADT-Tweak-End
        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        // ADT-Tweak-Start: метод сортировки, количество переноса и пресеты количеств
        [DataField]
        public int SortMethod;

        [DataField]
        public int TransferringAmount = 1;

        [DataField]
        public List<int> Amounts = new()
        {
            1, 5, 10, 15, 20, 25, 30, 50
        };

        // ADT-Tweak: Pill container storage - 3 containers with 10 slots each (3x10 grid)
        [DataField]
        public List<EntityUid?> StoredPillContainers = new();

        [DataField]
        public int SelectedPillContainerSlot = -1;

        [DataField]
        public int SelectedPillContainerForFill = -1;

        [DataField]
        public int SelectedPillCanisterForCreation = -1;

        [DataField]
        public ReagentId? SelectedReagent = null;

        // ADT-Tweak: Legacy bottle storage
        [DataField]
        public List<EntityUid?> StoredBottles = new();

        [DataField]
        public int SelectedBottleSlot = -1;

        [DataField]
        public int SelectedBottleForFill = -1;

        [DataField]
        public List<ReagentId> SelectedReagents = new();

        // ADT-Tweak: Reagent selection amounts tracking
        [DataField]
        public Dictionary<ReagentId, float> SelectedReagentAmounts { get; set; } = new();
        // ADT-Tweak-End
    }
}
