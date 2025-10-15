using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects
{
    public sealed partial class PlaySoundEffect : EntityEffect
    {
        [DataField(required: true)]
        public SoundSpecifier Sound;

        // JUSTIFICATION: This is purely cosmetic.
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => null;

        // Why do I even need to do this???
        public override void Effect(EntityEffectBaseArgs args)
        {
            var transform = args.EntityManager.GetComponent<TransformComponent>(args.TargetEntity);
            var audioSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedAudioSystem>();

            audioSys.PlayPredicted(Sound, transform.Coordinates, args.TargetEntity);
        }
    }
}
