using Content.Shared.ADT.Construction.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Server.ADT.Chemistry.EntitySystems;
using Content.Shared.ADT.Chemistry;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Chemistry.Components
{
    /// <summary>
    /// A machine that dispenses reagents into a solution container from containers in its storage slots.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(EnergyReagentDispenserSystem))]
    public sealed partial class EnergyReagentDispenserComponent : Component
    {
        // ADT-Tweak: батарейка как машинная часть (ёмкость/заряд химки = батарейка из крафта)
        public const string PartContainerName = "machine_parts";

        [DataField]
        public ItemSlot EnergyBeakerSlot = new();

        [DataField]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        /// <summary>
        /// текущая выдача. Не забивайте голову и не трогайте
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public EnergyReagentDispenserDispenseAmount DispenseAmount = EnergyReagentDispenserDispenseAmount.U10;

        /// <summary>
        /// звук отсутствия энергии
        /// </summary>
        [DataField, ViewVariables]
        public SoundSpecifier PowerSound = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");

        /// <summary>
        /// Сами реагенты. Указываеть как (Айди): (цена)
        /// </summary>
        [DataField]
        public Dictionary<string, float> Reagents = [];

        /// <summary>
        /// добавление реагентов при емагу
        /// </summary>
        [DataField]
        public Dictionary<string, float>? ReagentsEmagged = [];

        /// <summary>
        /// при включении нельзя емагнуть
        /// </summary>
        [DataField]
        public bool Emagged = false;

        /// <summary>
        /// Нюкерская версия: всегда заряжена на 100% и не разряжается.
        /// </summary>
        [DataField]
        public bool InfiniteBattery = false;

        // ADT-Tweak-Start: machine parts with tiers
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseRechargeRate = 25f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float FinalRechargeRate = 25f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseEnergyCostMultiplier = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float FinalEnergyCostMultiplier = 1f;

        [DataField]
        public ProtoId<MachinePartPrototype> CapacitorPart = "Capacitor";

        [DataField]
        public ProtoId<MachinePartPrototype> MatterBinPart = "MatterBin";

        [DataField]
        public ProtoId<MachinePartPrototype> ServoPart = "Servo";

        /// <summary>
        /// Зарядка батареи в секунду для Т2 (Т3 = x2, Т4 = x4). Т1 без авто-зарядки.
        /// </summary>
        [DataField]
        public float RechargeRatePerTier = 5f;

        /// <summary>
        /// Реагенты, разблокируемые серво по тирам (цена 8 W/u за единицу).
        /// </summary>
        [DataField]
        public Dictionary<int, List<string>> TierReagents = new()
        {
            [1] = ["TableSalt", "Ash", "WeldingFuel"],
            [2] = ["Acetone", "Ammonia"],
            [3] = ["Toxin", "Phenol"],
            [4] = ["Diethylamine"],
        };

        /// <summary>
        /// Цена разблокируемых реагентов за единицу.
        /// </summary>
        [DataField]
        public float TierReagentCost = 8f;
        // ADT-Tweak-End
    }
}
