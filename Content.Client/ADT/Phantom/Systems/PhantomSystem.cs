using Content.Shared.ADT.Phantom;
using Content.Shared.ADT.Phantom.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.ADT.Phantom;

public sealed partial class PhantomSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeUI();

        SubscribeLocalEvent<PhantomComponent, AppearanceChangeEvent>(OnAppearanceChange);

        SubscribeLocalEvent<VesselComponent, GetStatusIconsEvent>(GetVesselIcon);
        SubscribeLocalEvent<PhantomPuppetComponent, GetStatusIconsEvent>(GetPuppetIcon);
        SubscribeLocalEvent<PhantomImmuneComponent, GetStatusIconsEvent>(GetImmuneIcon);
    }

    private void GetVesselIcon(Entity<VesselComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<PhantomPuppetComponent>(ent))
            return;

        if (_proto.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetPuppetIcon(Entity<PhantomPuppetComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_proto.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetImmuneIcon(Entity<PhantomImmuneComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_proto.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void OnAppearanceChange(EntityUid uid, PhantomComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, PhantomVisuals.Haunting, out var haunt, args.Component))
            args.Sprite.LayerSetState(0, haunt ? component.HauntingState : component.State);

        else if (_appearance.TryGetData<bool>(uid, PhantomVisuals.Stunned, out var stunned, args.Component) && stunned)
            args.Sprite.LayerSetState(0, stunned ? component.StunnedState : component.State);

        else if (_appearance.TryGetData<bool>(uid, PhantomVisuals.Corporeal, out var corporeal, args.Component))
            args.Sprite.LayerSetState(0, corporeal ? component.CorporealState : component.State);
    }
}
