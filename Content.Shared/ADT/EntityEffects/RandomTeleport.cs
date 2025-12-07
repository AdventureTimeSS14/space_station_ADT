using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects
{
    /// <summary>
    /// Default metabolism for stimulants and tranqs. Attempts to find a MovementSpeedModifier on the target,
    /// adding one if not there and to change the movespeed
    /// </summary>
    public sealed partial class RandomTeleport : EntityEffectBase<RandomTeleport>
    {
        public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            return Loc.GetString("reagent-effect-guidebook-teleport",
                ("chance", Probability));
        }
    }
}
