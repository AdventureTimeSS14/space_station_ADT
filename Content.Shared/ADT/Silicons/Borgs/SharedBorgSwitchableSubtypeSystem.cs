using Robust.Shared.Prototypes;
using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared.ADT.Silicons.Borgs;

public abstract class SharedBorgSwitchableSubtypeSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager Prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableSubtypeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BorgSwitchableSubtypeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BorgSwitchableSubtypeComponent, AfterBorgTypeSelectEvent>(OnBorgTypeSelect);
    }

    private void OnMapInit(Entity<BorgSwitchableSubtypeComponent> ent, ref MapInitEvent args)
    {
        UpdateVisuals(ent);
    }

    private void OnComponentInit(Entity<BorgSwitchableSubtypeComponent> ent, ref ComponentInit args)
    {
        UpdateVisuals(ent);
    }

    private void OnBorgTypeSelect(Entity<BorgSwitchableSubtypeComponent> ent, ref AfterBorgTypeSelectEvent args)
    {
        if (ent.Comp.BorgSubtype == null)
            return;

        Dirty(ent);
        UpdateVisuals(ent);
    }

    protected virtual void SetAppearanceFromSubtype(Entity<BorgSwitchableSubtypeComponent> ent, ProtoId<BorgSubtypePrototype> subtype) { }

    protected void UpdateVisuals(Entity<BorgSwitchableSubtypeComponent> ent)
    {
        if (ent.Comp.BorgSubtype == null)
            return;
        SetAppearanceFromSubtype(ent, ent.Comp.BorgSubtype.Value);
    }
}

[ByRefEvent]
public record struct AfterBorgTypeSelectEvent;
