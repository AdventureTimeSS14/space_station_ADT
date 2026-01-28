using Content.Shared.ADT.PunchingBag;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;

namespace Content.Server.ADT.PunchingBag;

public sealed class PunchingBagAnimationsSystem : SharedPunchingBagAnimationsSystem
{
    protected override void PlayAnimation(EntityUid uid, EntityUid attacker, string animationState)
    {
        var filter = Filter.Pvs(uid, entityManager: EntityManager);

        if (TryComp(attacker, out ActorComponent? actor))
            filter.RemovePlayer(actor.PlayerSession);

        RaiseNetworkEvent(new PunchingBagAnimationEvent(GetNetEntity(uid), animationState), filter);

        PullerComponent? pullerComp = null;
        if (Resolve(attacker, ref pullerComp))
        {
            pullerComp.PulledDensityReduction = Math.Min(pullerComp.PulledDensityReduction + 0.01f, 0.8f);
            Dirty(attacker, pullerComp);
        }
    }
}

