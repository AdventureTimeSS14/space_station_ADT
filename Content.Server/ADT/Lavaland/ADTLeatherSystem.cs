using Content.Server.Stack;
using Content.Shared.ADT.Lavaland;
using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTLeatherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly StackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDehairableComponent, InteractUsingEvent>(OnDehairInteract);
        SubscribeLocalEvent<ADTDehairableComponent, ADTDehairDoAfterEvent>(OnDehairDoAfter);

        SubscribeLocalEvent<ADTSoakableComponent, InteractUsingEvent>(OnSoakInteract);
        SubscribeLocalEvent<ADTSoakableComponent, AfterInteractEvent>(OnSoakAfterInteract);
        SubscribeLocalEvent<ADTSoakableComponent, ReactionEntityEvent>(OnSoakReaction);

        SubscribeLocalEvent<ADTDryableComponent, MapInitEvent>(OnDryableMapInit);
        SubscribeLocalEvent<ADTDryableComponent, AtmosExposedUpdateEvent>(OnDryableExposed);

        SubscribeLocalEvent<ADTDryingRackComponent, MapInitEvent>(OnRackMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTDryingRackComponent>();

        while (query.MoveNext(out var uid, out var rack))
        {
            if (now < rack.NextUpdate)
                continue;

            rack.NextUpdate = now + rack.Interval;
            DryOnRack((uid, rack));
        }
    }

    private void OnDehairInteract(Entity<ADTDehairableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<SharpComponent>(args.Used))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, new ADTDehairDoAfterEvent(), ent.Owner, ent.Owner, args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _audio.PlayPvs(ent.Comp.Sound, ent.Owner);
        _popup.PopupEntity(Loc.GetString("adt-leather-dehair-start", ("target", ent.Owner)), args.User, args.User);
        args.Handled = true;
    }

    private void OnDehairDoAfter(Entity<ADTDehairableComponent> ent, ref ADTDehairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        ReplaceWith(ent.Owner, ent.Comp.Result, ent.Comp.Multiplier);
    }

    private void OnSoakInteract(Entity<ADTSoakableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TrySoakFrom(ent, args.Used, args.User);
    }

    private void OnSoakAfterInteract(Entity<ADTSoakableComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        args.Handled = TrySoakFrom(ent, target, args.User);
    }

    private bool TrySoakFrom(Entity<ADTSoakableComponent> ent, EntityUid source, EntityUid user)
    {
        if (!_solution.TryGetDrainableSolution(source, out var soln, out var solution))
            return false;

        if (solution.GetTotalPrototypeQuantity(ent.Comp.Reagent) < ent.Comp.Amount)
        {
            _popup.PopupEntity(Loc.GetString("adt-leather-soak-not-enough", ("source", source)), user, user);
            return true;
        }

        _solution.RemoveReagent(soln.Value, ent.Comp.Reagent.Id, ent.Comp.Amount);
        _popup.PopupEntity(Loc.GetString("adt-leather-soak", ("target", ent.Owner)), user, user);
        ReplaceWith(ent.Owner, ent.Comp.Result);
        return true;
    }

    private void OnSoakReaction(Entity<ADTSoakableComponent> ent, ref ReactionEntityEvent args)
    {
        if (args.Method != ReactionMethod.Touch || args.Reagent.ID != ent.Comp.Reagent.Id)
            return;

        if (args.ReagentQuantity.Quantity < ent.Comp.Amount)
            return;

        ReplaceWith(ent.Owner, ent.Comp.Result);
    }

    private void OnDryableMapInit(Entity<ADTDryableComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Remaining = ent.Comp.Wetness;
    }

    private void OnDryableExposed(Entity<ADTDryableComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        if (args.GasMixture.Temperature < ent.Comp.DryingTemperature)
            return;

        ent.Comp.Remaining--;

        if (ent.Comp.Remaining > 0)
            return;

        ent.Comp.Remaining = ent.Comp.Wetness;
        DryOne(ent);
    }

    private void OnRackMapInit(Entity<ADTDryingRackComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.Interval;
    }

    private void DryOnRack(Entity<ADTDryingRackComponent> rack)
    {
        if (!_container.TryGetContainer(rack.Owner, rack.Comp.Container, out var container))
            return;

        foreach (var item in container.ContainedEntities)
        {
            if (!TryComp<ADTDryableComponent>(item, out var dryable))
                continue;

            var count = TryComp<StackComponent>(item, out var stack) ? stack.Count : 1;
            _stack.SpawnMultipleAtPosition(dryable.Result, count, Transform(rack.Owner).Coordinates);
            QueueDel(item);
            return;
        }
    }

    private void DryOne(Entity<ADTDryableComponent> ent)
    {
        if (EntityManager.IsQueuedForDeletion(ent.Owner))
            return;

        _stack.SpawnMultipleNextToOrDrop(ent.Comp.Result, 1, ent.Owner);

        if (TryComp<StackComponent>(ent.Owner, out var stack) && stack.Count > 1)
        {
            _stack.SetCount((ent.Owner, stack), stack.Count - 1);
            return;
        }

        QueueDel(ent.Owner);
    }

    private void ReplaceWith(EntityUid uid, EntProtoId result, int multiplier = 1)
    {
        if (EntityManager.IsQueuedForDeletion(uid))
            return;

        var count = TryComp<StackComponent>(uid, out var stack) ? stack.Count : 1;

        _stack.SpawnMultipleNextToOrDrop(result, count * multiplier, uid);
        QueueDel(uid);
    }
}
