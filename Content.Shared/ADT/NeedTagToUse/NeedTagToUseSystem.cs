using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Tag;

namespace Content.Shared.ADT.NeedTagToUse
{
    public sealed class NeedTagToUseSystem : EntitySystem
    {
        [Dependency] private readonly TagSystem _tagSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<NeedTagToUseComponent, InteractionAttemptEvent>(OnInteractionAttempt);
            SubscribeLocalEvent<NeedTagToUseComponent, PickupAttemptEvent>(OnAttempt);
        }

        private void OnInteractionAttempt(EntityUid uid, NeedTagToUseComponent component, InteractionAttemptEvent args)
        {
            if (args.Target != null && !HasComp<UnremoveableComponent>(args.Target))
                args.Cancelled = true;

            if (HasComp<ItemComponent>(args.Target) && !HasComp<UnremoveableComponent>(args.Target))
            {
                if (!_tagSystem.HasAnyTag(args.Target.Value, component.Tag))
                    args.Cancelled = true;
            }
        }
        private void OnAttempt(EntityUid uid, NeedTagToUseComponent component, PickupAttemptEvent args)
        {
            if (!_tagSystem.HasAnyTag(args.Item, component.Tag))
                args.Cancel();
        }
    }
}
