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

        // ADT-Tweak-Start: dosage limit for bottles, maximum number of tablets and bottles
        [DataField("bottleDosageLimit", required: true), ViewVariables(VVAccess.ReadWrite)]
        public uint BottleDosageLimit;

        //
        [DataField("maxPills", required: true), ViewVariables(VVAccess.ReadWrite)]
        public uint MaxPills = 30;

        [DataField("maxBottles", required: true), ViewVariables(VVAccess.ReadWrite)]
        public uint MaxBottles = 20;
        // ADT-Tweak-End
        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        // ADT-Tweak-Start

        [DataField]
        public int SortMethod;

        [DataField]
        public int TransferringAmount = 1;

        [DataField]
        public List<int> Amounts = new()
        {
            1, 5, 10, 15, 20, 30, 50, 100, 200, 300, 500, 1000
        };

        // Pill container storage - 3 containers with 10 slots each (3x10 grid)
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

        // Bottle container storage - 4x5 grid
        [DataField]
        public List<EntityUid?> StoredBottles = new();

        [DataField]
        public int SelectedBottleForFill = -1;

        [DataField]
        public List<ReagentId> SelectedReagents = new();

        // Reagent selection amounts tracking
        [DataField]
        public Dictionary<ReagentId, float> SelectedReagentAmounts { get; set; } = new();
        // ADT-Tweak-End
    }
}
