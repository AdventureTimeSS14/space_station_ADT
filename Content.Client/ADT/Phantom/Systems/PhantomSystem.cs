using Content.Shared.ADT.Phantom;
using Content.Shared.ADT.Phantom.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Client.UserInterface;
using Content.Shared.StatusIcon.Components;
using Content.Client.Humanoid;

namespace Content.Client.ADT.Phantom;

public sealed class PhantomSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhantomComponent, AppearanceChangeEvent>(OnAppearanceChange);

        SubscribeLocalEvent<VesselComponent, GetStatusIconsEvent>(GetVesselIcon);
        SubscribeLocalEvent<PhantomPuppetComponent, GetStatusIconsEvent>(GetPuppetIcon);
        // SubscribeLocalEvent<PhantomHolderComponent, GetStatusIconsEvent>(GetHauntedIcon);
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

    // private void GetHauntedIcon(Entity<PhantomHolderComponent> ent, ref GetStatusIconsEvent args)
    // {
    //     if (_proto.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
    //         args.StatusIcons.Add(iconPrototype);
    // }

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
        {
            if (haunt)
                args.Sprite.LayerSetState(0, component.HauntingState);
            else
                args.Sprite.LayerSetState(0, component.State);
        }
        else if (_appearance.TryGetData<bool>(uid, PhantomVisuals.Stunned, out var stunned, args.Component) && stunned)
        {
            args.Sprite.LayerSetState(0, component.StunnedState);
        }
        else if (_appearance.TryGetData<bool>(uid, PhantomVisuals.Corporeal, out var corporeal, args.Component))
        {
            if (corporeal)
                args.Sprite.LayerSetState(0, component.CorporealState);
            else
                args.Sprite.LayerSetState(0, component.State);
        }
    }
}
