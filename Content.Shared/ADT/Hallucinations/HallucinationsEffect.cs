using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects
{
    /// <summary>
    /// Default metabolism for stimulants and tranqs. Attempts to find a MovementSpeedModifier on the target,
    /// adding one if not there and to change the movespeed
    /// </summary>
    public sealed partial class HallucinationsReagentEffect : EventEntityEffect<HallucinationsReagentEffect>
    {
        [DataField("key")]
        public string Key = "ADTHallucinations";

        [DataField(required: true)]
        public string Proto = string.Empty;

        [DataField]
        public float Time = 2.0f;

        [DataField]
        public bool Refresh = true;

        [DataField]
        public HallucinationsMetabolismType Type = HallucinationsMetabolismType.Add;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            return Loc.GetString("reagent-effect-guidebook-hallucinations",
                ("chance", Probability),
                ("time", Time));
        }
    }

    public enum HallucinationsMetabolismType
    {
        Add,
        Remove,
        Set
    }
}
