using Content.Server.ADT.Heretic.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Server.ADT.Heretic.EntitySystems;

// ADT: перенесено из Content.Goobstation.Server.ComponentsRegistry
public sealed partial class GrantComponentsStatusEffectSystem : EntitySystem
{
    // please don't use it for anything more complicated than adding immunity to stuff.

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrantComponentsStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApply);
        SubscribeLocalEvent<GrantComponentsStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemove);
    }

    private void OnStatusEffectApply(Entity<GrantComponentsStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        EntityManager.AddComponents(args.Target, ent.Comp.Components);
    }

    private void OnStatusEffectRemove(Entity<GrantComponentsStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        EntityManager.RemoveComponents(args.Target, ent.Comp.Components);
    }
}
