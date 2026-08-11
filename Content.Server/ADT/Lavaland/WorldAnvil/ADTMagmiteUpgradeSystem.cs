using System.Linq;
using Content.Shared.ADT.Lavaland.WorldAnvil;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Spawners;

namespace Content.Server.ADT.Lavaland.WorldAnvil;

public sealed class ADTMagmiteUpgradeSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMagmiteUpgradeComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ADTMagmiteUpgradeComponent, ADTMagmiteUpgradeDoAfterEvent>(OnUpgradeDoAfter);
        SubscribeLocalEvent<ADTMagmiteUpgradeComponent, TimedDespawnEvent>(OnCooled);
    }

    private void OnCooled(Entity<ADTMagmiteUpgradeComponent> parts, ref TimedDespawnEvent args)
    {
        _popup.PopupEntity(Loc.GetString(parts.Comp.CoolMessage), parts, PopupType.MediumCaution);
    }

    private void OnAfterInteract(Entity<ADTMagmiteUpgradeComponent> parts, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<ADTMagmiteUpgradableComponent>(target, out var upgradable))
            return;

        args.Handled = true;

        if (IsAlreadyUpgraded(target, upgradable))
        {
            _popup.PopupEntity(Loc.GetString("adt-magmite-parts-already-upgraded"), args.User, args.User);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager,
            args.User,
            parts.Comp.UpgradeDelay,
            new ADTMagmiteUpgradeDoAfterEvent(),
            parts,
            target: target,
            used: parts)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnUpgradeDoAfter(Entity<ADTMagmiteUpgradeComponent> parts, ref ADTMagmiteUpgradeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Target is not { } target || TerminatingOrDeleted(target))
            return;

        if (!TryComp<ADTMagmiteUpgradableComponent>(target, out var upgradable) || IsAlreadyUpgraded(target, upgradable))
            return;

        if (TryComp<ContainerManagerComponent>(target, out var containers))
        {
            foreach (var container in _container.GetAllContainers(target, containers).ToList())
            {
                _container.EmptyContainer(container);
            }
        }

        var inHands = _hands.IsHolding(args.User, target);
        var coordinates = inHands
            ? _transform.GetMoverCoordinates(args.User)
            : Transform(target).Coordinates;

        Del(target);
        QueueDel(parts);

        var upgraded = Spawn(upgradable.Result, coordinates);

        if (inHands)
            _hands.TryPickupAnyHand(args.User, upgraded);

        _popup.PopupEntity(Loc.GetString("adt-magmite-parts-upgraded"), args.User, args.User);

        args.Handled = true;
    }

    private bool IsAlreadyUpgraded(EntityUid target, ADTMagmiteUpgradableComponent upgradable)
    {
        return MetaData(target).EntityPrototype?.ID == upgradable.Result.Id;
    }
}
