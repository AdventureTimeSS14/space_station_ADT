using Content.Shared.ADT.Silicon;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Medical.Patch;

public sealed class ADTPatchSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTPatchComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ADTPatchComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ADTPatchComponent, ADTPatchApplyDoAfterEvent>(OnApplyDoAfter);
        SubscribeLocalEvent<ADTPatchComponent, EntGotRemovedFromContainerMessage>(OnPatchRemoved);

        SubscribeLocalEvent<ADTPatchedComponent, ComponentInit>(OnPatchedInit);
        SubscribeLocalEvent<ADTPatchedComponent, ComponentShutdown>(OnPatchedShutdown);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTPatchComponent>();
        while (query.MoveNext(out var uid, out var patch))
        {
            if (patch.AppliedTo is not { } target)
                continue;

            if (time < patch.NextTransfer)
                continue;

            patch.NextTransfer = time + patch.TransferDelay;
            Dirty(uid, patch);

            if (TerminatingOrDeleted(target) || !TryComp<BloodstreamComponent>(target, out var bloodstream))
                continue;

            if (!_solutionContainer.TryGetSolution(uid, patch.Solution, out var soln, out var solution) ||
                solution.Volume <= FixedPoint2.Zero)
                continue;

            var multiplier = GetStackMultiplier((uid, patch), target);
            var amount = FixedPoint2.New(patch.TransferRate.Float() * (float) patch.TransferDelay.TotalSeconds * multiplier);
            amount = FixedPoint2.Min(amount, solution.Volume);

            if (amount <= FixedPoint2.Zero)
                continue;

            var seeped = _solutionContainer.SplitSolution(soln.Value, amount);

            if (!_bloodstream.TryAddToBloodstream((target, bloodstream), seeped))
            {
                _solutionContainer.TryAddSolution(soln.Value, seeped);
                continue;
            }

            _reactive.DoEntityReaction(target, seeped, ReactionMethod.Injection);
        }
    }

    private void OnPatchedInit(Entity<ADTPatchedComponent> ent, ref ComponentInit args)
    {
        ent.Comp.PatchContainer = _container.EnsureContainer<Container>(ent.Owner, ADTPatchedComponent.ContainerId);
        ent.Comp.PatchContainer.OccludesLight = false;
    }

    private void OnPatchedShutdown(Entity<ADTPatchedComponent> ent, ref ComponentShutdown args)
    {
        _container.CleanContainer(ent.Comp.PatchContainer);
    }

    private void OnAfterInteract(Entity<ADTPatchComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<MobStateComponent>(target))
            return;

        args.Handled = TryStartApply(ent, args.User, target);
    }

    private void OnUseInHand(Entity<ADTPatchComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartApply(ent, args.User, args.User);
    }

    private void OnApplyDoAfter(Entity<ADTPatchComponent> ent, ref ADTPatchApplyDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (ent.Comp.AppliedTo != null || !CanApply(ent, args.User, target))
            return;

        Apply(ent, args.User, target);
    }

    private void OnPatchRemoved(Entity<ADTPatchComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ADTPatchedComponent.ContainerId)
            return;

        ent.Comp.AppliedTo = null;
        Dirty(ent);
    }

    private bool TryStartApply(Entity<ADTPatchComponent> ent, EntityUid user, EntityUid target)
    {
        if (ent.Comp.AppliedTo != null)
            return false;

        if (!CanApply(ent, user, target))
            return false;

        if (user == target || ent.Comp.Delay <= TimeSpan.Zero)
        {
            Apply(ent, user, target);
            return true;
        }

        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.Delay, new ADTPatchApplyDoAfterEvent(), ent, target, ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return false;

        _popup.PopupPredicted(
            Loc.GetString("adt-patch-apply-attempt-user", ("patch", ent.Owner), ("target", Identity.Entity(target, EntityManager))),
            Loc.GetString("adt-patch-apply-attempt-others", ("patch", ent.Owner), ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager))),
            target,
            user);

        return true;
    }

    private bool CanApply(Entity<ADTPatchComponent> ent, EntityUid user, EntityUid target)
    {
        if (!HasComp<BloodstreamComponent>(target) || HasComp<MobIpcComponent>(target))
        {
            _popup.PopupClient(
                Loc.GetString("adt-patch-invalid-target", ("patch", ent.Owner), ("target", Identity.Entity(target, EntityManager))),
                ent,
                user);
            return false;
        }

        return true;
    }

    private void Apply(Entity<ADTPatchComponent> ent, EntityUid user, EntityUid target)
    {
        var patched = EnsureComp<ADTPatchedComponent>(target);

        if (!_container.Insert(ent.Owner, patched.PatchContainer))
            return;

        ent.Comp.AppliedTo = target;
        ent.Comp.NextTransfer = _timing.CurTime + ent.Comp.TransferDelay;
        Dirty(ent);

        _audio.PlayPredicted(ent.Comp.ApplySound, target, user);

        if (user == target)
        {
            _popup.PopupPredicted(
                Loc.GetString("adt-patch-apply-self", ("patch", ent.Owner)),
                Loc.GetString("adt-patch-apply-self-others", ("patch", ent.Owner), ("user", Identity.Entity(user, EntityManager))),
                target,
                user);
            return;
        }

        _popup.PopupPredicted(
            Loc.GetString("adt-patch-apply-user", ("patch", ent.Owner), ("target", Identity.Entity(target, EntityManager))),
            Loc.GetString("adt-patch-apply-others", ("patch", ent.Owner), ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager))),
            target,
            user);
    }

    private float GetStackMultiplier(Entity<ADTPatchComponent> ent, EntityUid target)
    {
        if (!TryComp<ADTPatchedComponent>(target, out var patched))
            return 1f;

        var count = patched.PatchContainer.ContainedEntities.Count;
        return count > 1 ? count * ent.Comp.StackMultiplier : 1f;
    }
}

[Serializable, NetSerializable]
public sealed partial class ADTPatchApplyDoAfterEvent : SimpleDoAfterEvent
{
}
