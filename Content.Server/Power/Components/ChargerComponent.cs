using Content.Shared.Power;
using Content.Shared.Whitelist;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public sealed partial class ChargerComponent : Component
    {
        [ViewVariables]
        public CellChargerStatus Status;

        /// <summary>
        /// The charge rate of the charger, in watts
        /// </summary>
        [DataField("chargeRate")]
        public float ChargeRate = 20.0f;

        /// <summary>
        /// The container ID that is holds the entities being charged.
        /// </summary>
        [DataField("slotId", required: true)]
        public string SlotId = string.Empty;

        /// <summary>
        /// A whitelist for what entities can be charged by this Charger.
        /// </summary>
        [DataField("whitelist")]
        public EntityWhitelist? Whitelist;

        /// <summary>
        /// Indicates whether the charger is portable and thus subject to EMP effects
        /// and bypasses checks for transform, anchored, and ApcPowerReceiverComponent.
        /// </summary>
        [DataField]
        public bool Portable = false;

        // ADT-Tweak-Start

        /// <summary>
        /// The temperature the charger will stop heating up at.
        /// </summary>
        /// <remarks>
        /// Used specifically for chargers with the <see cref="SharedEntityStorageComponent"/>.
        /// </remarks>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float TargetTemp = 373.15f;

        /// <summary>
        /// To blow up or not to blow up... that is the question.
        /// </summary>
        [DataField]
        public bool BlowUp = false;

        /// <summary>
        ///     The minimum size of a battery to be charged.
        /// </summary>
        /// <remarks>
        ///     Charging a battery too small will detonate it, becoming more likely as it fills.
        /// </remarks>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public int MinChargeSize = 700;
        // ADT-Tweak-End
    }
}
