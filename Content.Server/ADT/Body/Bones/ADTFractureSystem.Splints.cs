using Content.Shared.ADT.Body.Bones;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Player;

namespace Content.Server.ADT.Body.Bones;

public sealed partial class ADTFractureSystem
{
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private ADTBodyActionsSystem _bodyActions = default!;

    private static readonly TimeSpan SplintRemoveDelay = TimeSpan.FromSeconds(3);

    private void InitializeSplints()
    {
        SubscribeLocalEvent<ADTSplintComponent, AfterInteractEvent>(OnSplintAfterInteract);
        SubscribeLocalEvent<ADTSplintComponent, UseInHandEvent>(OnSplintUseInHand);
        SubscribeLocalEvent<ADTSplintComponent, ADTSplintDoAfterEvent>(OnSplintDoAfter);
        SubscribeLocalEvent<ADTFracturedComponent, ADTSplintRemoveDoAfterEvent>(OnSplintRemoveDoAfter);
    }

    public bool CanSplint(Entity<ADTBoneComponent> bone)
    {
        return bone.Comp.CanSplint
               && !bone.Comp.Splinted
               && GetFractureType(bone.Comp) is { CanSplint: true };
    }

    public bool TrySplint(EntityUid body, Entity<ADTBoneComponent> bone, float durability)
    {
        if (!CanSplint(bone))
            return false;

        SetSplinted(bone, true, durability);
        Refresh(body);
        return true;
    }

    public void RemoveSplint(EntityUid body, Entity<ADTBoneComponent> bone, bool broke)
    {
        if (!bone.Comp.Splinted)
            return;

        SetSplinted(bone, false, 0f);
        Refresh(body);

        if (!broke)
            return;

        if (TryComp<ADTBonesComponent>(body, out var bones))
            _stun.TryAddStunDuration(body, bones.SplintBreakStun);

        _popup.PopupEntity(Loc.GetString("adt-splint-broke-self", ("limb", bone.Owner)), body, body, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("adt-splint-broke-others", ("target", body), ("limb", bone.Owner)), body, Filter.PvsExcept(body), true, PopupType.MediumCaution);
        TryScream(body);
    }

    private void OnSplintAfterInteract(Entity<ADTSplintComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target || !HasComp<ADTBonesComponent>(target))
            return;

        args.Handled = true;
        _bodyActions.Open(args.User, target);
    }

    private void OnSplintUseInHand(Entity<ADTSplintComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !HasComp<ADTBonesComponent>(args.User))
            return;

        args.Handled = true;
        _bodyActions.Open(args.User, args.User);
    }

    public void StartSplint(Entity<ADTSplintComponent> ent, EntityUid user, EntityUid target, EntityUid organ)
    {
        var delay = user == target ? ent.Comp.SelfDelay : ent.Comp.OtherDelay;
        var args = new DoAfterArgs(EntityManager, user, delay, new ADTSplintDoAfterEvent(GetNetEntity(organ)), ent, target, ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        if (user != target)
            _popup.PopupEntity(Loc.GetString("adt-splint-start", ("user", user), ("limb", organ)), target, target, PopupType.Medium);
    }

    private void OnSplintDoAfter(Entity<ADTSplintComponent> ent, ref ADTSplintDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        var organ = GetEntity(args.Organ);
        if (!TryComp<ADTBoneComponent>(organ, out var bone) || CompOrNull<OrganComponent>(organ)?.Body != target)
            return;

        if (!TrySplint(target, (organ, bone), ent.Comp.Durability))
            return;

        _popup.PopupEntity(Loc.GetString("adt-splint-applied", ("user", args.User), ("limb", organ)), target, PopupType.Medium);

        if (TryComp<StackComponent>(ent, out var stack))
            _stack.TryUse((ent.Owner, stack), 1);
        else
            QueueDel(ent);
    }

    public void StartSplintRemove(EntityUid user, EntityUid body, EntityUid organ)
    {
        var args = new DoAfterArgs(EntityManager, user, SplintRemoveDelay, new ADTSplintRemoveDoAfterEvent(GetNetEntity(organ)), body, body)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnSplintRemoveDoAfter(Entity<ADTFracturedComponent> ent, ref ADTSplintRemoveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var organ = GetEntity(args.Organ);
        if (!TryComp<ADTBoneComponent>(organ, out var bone) || CompOrNull<OrganComponent>(organ)?.Body != ent.Owner)
            return;

        RemoveSplint(ent, (organ, bone), false);
        _popup.PopupEntity(Loc.GetString("adt-splint-removed", ("limb", organ)), ent, args.User);
    }
}
