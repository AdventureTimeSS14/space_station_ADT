using Content.Shared.StatusEffectNew;
using Content.Shared.ADT.ComponentsRegistry;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.ComponentsRegistry;

public sealed partial class GrantComponentsStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrantComponentsStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApply);
        SubscribeLocalEvent<GrantComponentsStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemove);
    }

    private void OnStatusEffectApply(Entity<GrantComponentsStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        var missing = new ComponentRegistry();
        foreach (var (name, entry) in ent.Comp.Components)
        {
            if (HasComp(args.Target, entry.Component.GetType()))
                continue;

            missing.Add(name, entry);
            ent.Comp.Added.Add(name);
        }

        EntityManager.AddComponents(args.Target, missing);
    }

    private void OnStatusEffectRemove(Entity<GrantComponentsStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        var added = new ComponentRegistry();
        foreach (var name in ent.Comp.Added)
        {
            added.Add(name, ent.Comp.Components[name]);
        }

        ent.Comp.Added.Clear();
        EntityManager.RemoveComponents(args.Target, added);
    }
}