using System.Threading;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Felinid
{
    [RegisterComponent]
    [Access(typeof(WoundLickingSystem))]
    public sealed partial class WoundLickingComponent : Component
    {
        [DataField("woundLickingAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? WoundLickingAction = "ActionWoundLicking";

        [DataField("woundLickingActionEntity")]
        public EntityUid? WoundLickingActionEntity;

        /// <summary>
        /// How frequent wound-licking will cause diseases. Scales with amount of reduced bleeding
        /// </summary>
        [DataField("diseaseChance")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float DiseaseChance { get; set; } = 0.25f;

        /// <summary>
        /// Max possible bleeding reduce. Human max bleeding is 20f, many weapons deals near 15f bleeding
        /// </summary>
        [DataField("maxHeal")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxHeal { get; set; } = 15f;

        /// <summary>
        /// How long it requires to lick wounds
        /// </summary>
        [DataField("delay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Delay { get; set; } = 3f;

        /// <summary>
        /// If true, then wound-licking can be applied only on yourself
        /// </summary>
        [DataField("canApplyOnSelf")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanApplyOnSelf { get; set; } = true;

        /// <summary>
        /// If true, then wound-licking can be applied only on other entities
        /// </summary>
        [DataField("canApplyOnOther")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanApplyOnOther { get; set; } = false;



        /// <summary>
        /// Which diseases can be caused because of wound-licking
        /// </summary>
        [DataField("possibleDiseases")]
        public List<String> PossibleDiseases { get; set; } = new()
        {
            "Plague",
            "BirdFlew",
            "SpaceFlu",
            "SpaceCold",
            "VentCough"
        };

        /// <summary>
        /// If Target's bloodstream don't use one of these reagents, then ability can't be performed on it.
        /// </summary>
        [DataField("reagentWhitelist")]
        public List<String> ReagentWhitelist { get; set; } = new()
        {
            "Blood",
            "Slime"
        };

        /// <summary>
        ///     Token for interrupting a do-after action. If not null, implies component is
        ///     currently "in use".
        /// </summary>
        public CancellationTokenSource? CancelToken;
    }
}
