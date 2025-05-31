using Robust.Shared.Prototypes;
using Content.Shared.ADT.Silicons.Borgs.Components;

namespace Content.Shared.ADT.Silicons.Borgs;

public abstract class SharedBorgSwitchableSubtypeSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager Prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableSubtypeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BorgSwitchableSubtypeComponent, BorgSubtypeChangedEvent>(OnSubtypeChanged);
        SubscribeLocalEvent<BorgSwitchableSubtypeComponent, BorgSelectSubtypeMessage>(OnSubtypeSelection);
    }

    private void OnComponentInit(Entity<BorgSwitchableSubtypeComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.BorgSubtype.HasValue)
        {
            SetAppearanceFromSubtype(ent, ent.Comp.BorgSubtype.Value);
        }
    }

    private void OnSubtypeSelection(Entity<BorgSwitchableSubtypeComponent> ent, ref BorgSelectSubtypeMessage args)
    {
        if (ent.Comp.BorgSubtype != null)
            return;

        if (!Prototypes.HasIndex(args.Subtype))
            return;

        SetSubtype(ent, args.Subtype);
    }

    private void OnSubtypeChanged(Entity<BorgSwitchableSubtypeComponent> ent, ref BorgSubtypeChangedEvent args)
    {
        SetAppearanceFromSubtype(ent, args.Subtype);
    }

    protected void SetAppearanceFromSubtype(Entity<BorgSwitchableSubtypeComponent> ent)
    {
        if (!Prototypes.TryIndex(ent.Comp.BorgSubtype, out var proto))
            return;

        SetAppearanceFromSubtype(ent, proto);
    }

    protected virtual void SetAppearanceFromSubtype(Entity<BorgSwitchableSubtypeComponent> ent, ProtoId<BorgSubtypePrototype> subtype) { }

    public void SetSubtype(Entity<BorgSwitchableSubtypeComponent> ent, ProtoId<BorgSubtypePrototype> subtype)
    {
        ent.Comp.BorgSubtype = subtype;
        RaiseLocalEvent(ent, new BorgSubtypeChangedEvent(subtype));
    }
}
