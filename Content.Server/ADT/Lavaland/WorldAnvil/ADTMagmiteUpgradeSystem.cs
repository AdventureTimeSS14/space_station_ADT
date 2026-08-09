using System.Linq;
using Content.Shared.ADT.Lavaland.WorldAnvil;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Lavaland.WorldAnvil;

public sealed class ADTMagmiteUpgradeSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMagmiteUpgradeComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<ADTMagmiteUpgradeComponent> parts, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<ADTMagmiteUpgradableComponent>(target, out var upgradable))
            return;

        if (MetaData(target).EntityPrototype?.ID == upgradable.Result.Id)
        {
            _popup.PopupEntity(Loc.GetString("adt-magmite-parts-already-upgraded"), args.User, args.User);
            args.Handled = true;
            return;
        }

        foreach (var container in _container.GetAllContainers(target).ToList())
        {
            _container.EmptyContainer(container);
        }

        var coordinates = _transform.GetMoverCoordinates(args.User);

        QueueDel(target);
        QueueDel(parts);

        var upgraded = Spawn(upgradable.Result, coordinates);
        _hands.TryPickupAnyHand(args.User, upgraded);

        _popup.PopupEntity(Loc.GetString("adt-magmite-parts-upgraded"), args.User, args.User);

        args.Handled = true;
    }
}
