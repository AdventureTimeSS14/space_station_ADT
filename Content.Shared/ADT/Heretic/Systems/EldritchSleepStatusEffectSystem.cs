//

using System.Linq;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Heretic.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Heretic.Systems;

public sealed partial class EldritchSleepStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EldritchSleepStatusEffectComponent, StatusEffectAppliedEvent>(OnApply,
            before: new[] { typeof(SleepingSystem) });
        SubscribeLocalEvent<EldritchSleepStatusEffectComponent, StatusEffectRemovedEvent>(OnRemove);

        SubscribeLocalEvent<MetabolismModifierComponent, GetMetabolicMultiplierEvent>(OnGetMultiplier);
    }

    private void OnGetMultiplier(Entity<MetabolismModifierComponent> ent, ref GetMetabolicMultiplierEvent args)
    {
        args.Multiplier *= ent.Comp.Modifier;
    }

    private void OnRemove(Entity<EldritchSleepStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_net.IsClient)
            return;

        EntityManager.RemoveComponents(args.Target, ent.Comp.ComponentDifference);

        if (TryComp(args.Target, out BloodstreamComponent? blood))
            _bloodstream.FlushChemicals((args.Target, blood), 200);
    }

    private void OnApply(Entity<EldritchSleepStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_net.IsClient)
            return;

        var ev = new RejuvenateEvent();
        RaiseLocalEvent(args.Target, ev);

        var existing = EntityManager.GetComponents(args.Target);
        var existingNames = new HashSet<string>();
        foreach (var c in existing)
        {
            existingNames.Add(EntityManager.ComponentFactory.GetRegistration(c.GetType()).Name);
        }

        var difference = new ComponentRegistry();
        foreach (var (key, entry) in ent.Comp.ComponentsToAdd)
        {
            if (!existingNames.Contains(key))
                difference.Add(key, entry);
        }

        ent.Comp.ComponentDifference = difference;
        Dirty(ent);
        EntityManager.AddComponents(args.Target, ent.Comp.ComponentsToAdd);
    }
}
