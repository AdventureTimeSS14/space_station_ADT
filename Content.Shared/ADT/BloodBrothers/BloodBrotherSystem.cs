using Content.Shared.Standing;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.BloodBrothers
{
    public abstract class SharedBloodBrothersSystem : EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BloodBrotherComponent, MapInitEvent>(SetBrotherId);

            SubscribeLocalEvent<BloodBrotherComponent, ComponentStartup>(DirtyBrotherComps);
            SubscribeLocalEvent<BloodBrotherLeaderComponent, ComponentStartup>(DirtyBrotherComps);
        }

        private void DirtyBrotherComps<T>(EntityUid someUid, T someComp, ComponentStartup ev)
        {
            var broComps = AllEntityQuery<BloodBrotherComponent>();
            while (broComps.MoveNext(out var uid, out var comp))
            {
                Dirty(uid, comp);
            }

            var broLeadComps = AllEntityQuery<BloodBrotherLeaderComponent>();
            while (broLeadComps.MoveNext(out var uid, out var comp))
            {
                Dirty(uid, comp);
            }
        }
        private void SetBrotherId(EntityUid uid, BloodBrotherComponent comp, MapInitEvent args)
        {
            if (HasComp<BloodBrotherLeaderComponent>(uid))
            {
                comp.Leader = uid;
            }
        }
    }
}