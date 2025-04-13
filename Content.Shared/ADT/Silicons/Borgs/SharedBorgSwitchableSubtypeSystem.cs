using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Silicons.Borgs;

public abstract class SharedBorgSwitchableSubtypeSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager Prototypes = default!;
    [Dependency] private readonly SharedBorgSwitchableSubtypeSystem _subtypeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableSubtypeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BorgSwitchableSubtypeComponent, BorgSelectSubtypeMessage>(OnSubtypeSelection);
    }

    private void OnSubtypeSelection(Entity<BorgSwitchableSubtypeComponent> ent, ref BorgSelectSubtypeMessage args)
    {
        SetSubtype(ent, args.Subtype);
    }


    private void OnComponentInit(Entity<BorgSwitchableSubtypeComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.BorgSubtype == null)
            return;

        SetAppearanceFromSubtype(ent, ent.Comp.BorgSubtype.Value);
    }

    protected virtual void SetAppearanceFromSubtype(Entity<BorgSwitchableSubtypeComponent> ent, ProtoId<BorgSubtypePrototype> subtype)
    {
    }

    public void SetSubtype(EntityUid ent, ProtoId<BorgSubtypePrototype> subtype)
    {
        if (!TryComp(ent, out BorgSwitchableSubtypeComponent? subtypeComp))
            return;

        subtypeComp.BorgSubtype = subtype;
        RaiseLocalEvent(ent, new BorgSubtypeChangedEvent(subtype));
    }
}

[Virtual]
public class BorgSubtypeChangedEvent : EntityEventArgs
{
    public ProtoId<BorgSubtypePrototype> Subtype { get; }

    public BorgSubtypeChangedEvent(ProtoId<BorgSubtypePrototype> subtype)
    {
        Subtype = subtype;
    }
}