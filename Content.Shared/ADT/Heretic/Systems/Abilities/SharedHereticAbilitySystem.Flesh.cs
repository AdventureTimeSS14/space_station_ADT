using Content.Shared.ADT.Heretic.Components;
using Content.Shared.DoAfter;

using Content.Shared.Heretic;
using Content.Shared.Interaction;

namespace Content.Shared.ADT.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    protected virtual void SubscribeFlesh()
    {
        SubscribeLocalEvent<EventHereticFleshSurgery>(OnFleshSurgery);
        SubscribeLocalEvent<EventHereticFleshSurgeryDoAfter>(OnFleshSurgeryDoAfter);

        SubscribeLocalEvent<FleshPassiveComponent, ImmuneToPoisonDamageEvent>(OnPoisonImmune);

        // ADT: щитмед-хирургия вырезана, рука лечит только гулей через do-after
        SubscribeLocalEvent<FleshSurgeryComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnPoisonImmune(Entity<FleshPassiveComponent> ent, ref ImmuneToPoisonDamageEvent args)
    {
        args.Immune = true;
    }

    private void OnAfterInteract(Entity<FleshSurgeryComponent> ent, ref AfterInteractEvent args)
    {
        if (!HasComp<GhoulComponent>(args.Target))
            return;

        var dargs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.Delay,
            new EventHereticFleshSurgeryDoAfter(),
            args.User,
            args.Target,
            ent)
        {
            Hidden = true, // Hidden because it also has health analyzer do-after
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnHandChange = false,
            BreakOnDropItem = false,
            Broadcast = true,
        };

        if (DoAfter.TryStartDoAfter(dargs))
            args.Handled = true;
    }

    private void OnFleshSurgery(EventHereticFleshSurgery args)
    {
        var touch = GetTouchSpell<EventHereticFleshSurgery, FleshSurgeryComponent>(args.Performer, ref args);
        if (touch == null)
            return;

        EnsureComp<FleshSurgeryComponent>(touch.Value).Action = args.Action.Owner;
    }

    private void OnFleshSurgeryDoAfter(EventHereticFleshSurgeryDoAfter args)
    {
        if (args.Cancelled)
            return;

        if (args.Target == null) // shouldn't really happen. just in case
            return;

        if (!TryComp(args.Used, out FleshSurgeryComponent? surgery))
            return;

        InvokeTouchSpell<FleshSurgeryComponent>((args.Used.Value, surgery), args.User);
        IHateWoundMed(args.Target.Value, null, null, null, null, null, null);
        args.Handled = true;
    }
}
