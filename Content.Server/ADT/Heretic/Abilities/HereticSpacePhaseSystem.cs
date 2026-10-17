using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Shared.ADT.Heretic.Abilities;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Maps;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Heretic.Abilities;

public sealed partial class HereticSpacePhaseSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly PolymorphSystem _poly = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EventHereticSpacePhase>(OnPhase);
    }

    private void OnPhase(EventHereticSpacePhase args)
    {
        if (args.Handled)
            return;
        if (!_heretic.TryGetHereticComponent(args.Performer, out _, out _))
            return;

        var xform = Transform(args.Performer);
        bool isSpace;
        if (xform.GridUid == null)
            isSpace = true;
        else if (TryComp(xform.GridUid.Value, out MapGridComponent? grid))
            isSpace = _turf.IsSpace(_map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates));
        else
            isSpace = true;

        if (!isSpace)
        {
            _popup.PopupEntity(Loc.GetString("heretic-space-phase-need-space"), args.Performer, args.Performer);
            return;
        }

        args.Handled = true;
        var morphed = _poly.PolymorphEntity(args.Performer, "SpaceCrawlMoon");
        if (morphed == null)
            return;

        var body = morphed.Value;
        var hands = new List<EntityUid>();

        for (var i = 0; i < 2; i++)
            hands.Add(Spawn("HereticSpaceHands", Transform(body).Coordinates));

        GiveHands(body, hands);

        Timer.Spawn(200, () => GiveHands(body, hands));
    }

    private void GiveHands(EntityUid body, List<EntityUid> hands)
    {
        if (TerminatingOrDeleted(body) || !TryComp<HandsComponent>(body, out var handsComp))
            return;

        foreach (var hand in hands)
        {
            if (TerminatingOrDeleted(hand))
                continue;

            if (_container.IsEntityInContainer(hand))
                continue;

            _hands.TryForcePickupAnyHand(body, hand, checkActionBlocker: false);
        }
    }
}
