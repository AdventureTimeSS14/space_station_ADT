using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Botany
{
    /// <summary>
    /// Anything that can be used to cross-pollinate plants.
    /// </summary>
    [RegisterComponent]
    public sealed partial class BotanySwabComponent : Component
    {
        [DataField("swabDelay")]
        public float SwabDelay = 2f;

        /// <summary>
        /// SeedData from the first plant that got swabbed.
        /// </summary>
        public SeedData? SeedData;

        /// <summary>
        /// Allergic triggers from players swabbed.
        /// </summary>
        // ADT-Tweak-Start
        [DataField]
        public List<ProtoId<ReagentPrototype>>? AllergicTriggers;
        // ADT-Tweak-End
    }
}
