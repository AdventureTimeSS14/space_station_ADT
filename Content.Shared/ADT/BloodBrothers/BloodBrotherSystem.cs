using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Content.Shared.Mobs;
using Content.Shared.Flash;

namespace Content.Shared.ADT.BloodBrothers
{
    public abstract class SharedBloodBrothersSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly StandingStateSystem _standingState = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BloodBrotherComponent, ComponentStartup>(DirtyBrotherComps);
            SubscribeLocalEvent<BloodBrotherLeaderComponent, ComponentStartup>(DirtyBrotherComps);
        }

        private void DirtyBrotherComps<T>(EntityUid someUid, T someComp, ComponentStartup ev)
        {
            var revComps = AllEntityQuery<BloodBrotherComponent>();
            while (revComps.MoveNext(out var uid, out var comp))
            {
                Dirty(uid, comp);
            }

            var headRevComps = AllEntityQuery<BloodBrotherLeaderComponent>();
            while (headRevComps.MoveNext(out var uid, out var comp))
            {
                Dirty(uid, comp);
            }
        }
    }
}