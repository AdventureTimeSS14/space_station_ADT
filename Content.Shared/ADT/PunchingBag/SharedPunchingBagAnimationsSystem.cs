using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.PunchingBag;

public abstract class SharedPunchingBagAnimationsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PunchingBagAnimationsComponent, AttackedEvent>(OnAttacked);
    }

    private void OnAttacked(Entity<PunchingBagAnimationsComponent> ent, ref AttackedEvent args)
    {
        PlayAnimation(ent, args.User, ent.Comp.AnimationState);
    }

    protected abstract void PlayAnimation(EntityUid uid, EntityUid attacker, string animationState);
}

[Serializable, NetSerializable]
public sealed class PunchingBagAnimationEvent(NetEntity uid, string animationState) : EntityEventArgs
{
    public NetEntity Uid = uid;
    public string AnimationState = animationState;
}

