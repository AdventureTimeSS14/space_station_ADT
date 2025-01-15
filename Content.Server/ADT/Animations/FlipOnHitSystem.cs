using Content.Shared.Animations;
using Robust.Shared.Player;

namespace Content.Server.Animations;

public sealed class FlipOnHitSystem : SharedFlipOnHitSystem
{
    protected override void PlayAnimation(EntityUid user)
    {
        var filter = Filter.Pvs(user, entityManager: EntityManager);

        if (TryComp<ActorComponent>(user, out var actor))
            filter.RemovePlayer(actor.PlayerSession);

        RaiseNetworkEvent(new FlipOnHitEvent(GetNetEntity(user)), filter);
    }
}
