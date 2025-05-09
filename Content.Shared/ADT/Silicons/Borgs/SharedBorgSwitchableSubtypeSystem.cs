using Robust.Shared.Prototypes;
using Content.Shared.ADT.Silicons.Borgs.Components;

namespace Content.Shared.ADT.Silicons.Borgs;

public abstract class SharedBorgSwitchableSubtypeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

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

        if (!_prototypes.HasIndex(args.Subtype))
            return;

        SetSubtype(ent, args.Subtype);
    }

    private void OnSubtypeChanged(Entity<BorgSwitchableSubtypeComponent> ent, ref BorgSubtypeChangedEvent args)
    {
        SetAppearanceFromSubtype(ent, args.Subtype);
    }

    protected virtual void SetAppearanceFromSubtype(Entity<BorgSwitchableSubtypeComponent> ent, ProtoId<BorgSubtypePrototype> subtype) { }

    public void SetSubtype(Entity<BorgSwitchableSubtypeComponent> ent, ProtoId<BorgSubtypePrototype> subtype)
    {
        ent.Comp.BorgSubtype = subtype;
        var ev = new BorgSubtypeChangedEvent(subtype);
        RaiseLocalEvent(ent, ref ev);
    }
}
