using Robust.Shared.Prototypes;
using Content.Shared.StatusEffect;
using Content.Shared.EntityEffects;
using Content.Server.ADT.Shadekin;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Default metabolism for stimulants and tranqs. Attempts to find a MovementSpeedModifier on the target,
    /// adding one if not there and to change the movespeed
    /// </summary>
    public sealed partial class RandomTeleport : EntityEffect
    {
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            return Loc.GetString("reagent-effect-guidebook-teleport",
                ("chance", Probability));
        }

        public override void Effect(EntityEffectBaseArgs ev)
        {
            if (ev is not EntityEffectReagentArgs args)
                return;

            var statusSys = args.EntityManager.EntitySysManager.GetEntitySystem<StatusEffectsSystem>();
            var shadekin = args.EntityManager.EntitySysManager.GetEntitySystem<ShadekinSystem>();

            shadekin.TeleportRandomlyNoComp(args.TargetEntity, 2f);
        }
    }
}
