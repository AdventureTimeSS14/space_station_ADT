using Content.Shared.Containers.ItemSlots;
using Content.Server.ADT.Chemistry.EntitySystems;
using Content.Shared.ADT.Chemistry;
using Robust.Shared.Audio;

namespace Content.Server.ADT.Chemistry.Components
{
    /// <summary>
    /// A machine that dispenses reagents into a solution container from containers in its storage slots.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(EnergyReagentDispenserSystem))]
    public sealed partial class EnergyReagentDispenserComponent : Component
    {
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
    }
}
