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
        if (TryComp<HandsComponent>(body, out var handsComp))
        {
            if (handsComp.Hands.Count == 0)
            {
                _hands.AddHand((body, handsComp), "left", HandLocation.Left);
                _hands.AddHand((body, handsComp), "right", HandLocation.Right);
            }

            foreach (var handName in handsComp.Hands.Keys)
            {
                var handItem = Spawn("HereticSpaceHands", Transform(body).Coordinates);
                if (!_hands.TryPickup(body, handItem, handName, checkActionBlocker: false, handsComp: handsComp))
                {
                    QueueDel(handItem);
                }
            }
        }
    }
}
