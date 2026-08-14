using Content.Shared.ADT.AshWalker;
using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IgnitionSource.Components;
using Content.Shared.IgnitionSource.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Nutrition;
using Content.Shared.Popups;

namespace Content.Server.ADT.AshWalker;

public sealed class ADTIgniteSystem : EntitySystem
{
    [Dependency] private readonly MatchstickSystem _matchstick = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private const SlotFlags MouthSlots = SlotFlags.HEAD | SlotFlags.MASK;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTIgniteComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTIgniteComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ADTIgniteComponent, ADTIgniteActionEvent>(OnIgnite);
    }

    private void OnMapInit(Entity<ADTIgniteComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.ActionId);
    }

    private void OnShutdown(Entity<ADTIgniteComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ActionEntity);
    }

    private void OnIgnite(Entity<ADTIgniteComponent> ent, ADTIgniteActionEvent args)
    {
        if (args.Handled)
            return;

        var attempt = new IngestionAttemptEvent(MouthSlots);
        RaiseLocalEvent(ent.Owner, ref attempt);

        if (attempt.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("adt-ash-walker-ignite-maw-covered"), ent, ent);
            return;
        }

        var ember = SpawnAtPosition(ent.Comp.Ember, Transform(ent).Coordinates);

        if (TryComp<MatchstickComponent>(ember, out var matchstick))
            _matchstick.TryIgnite((ember, matchstick), ent);

        if (!_hands.TryPickupAnyHand(ent, ember))
        {
            QueueDel(ember);
            _popup.PopupEntity(Loc.GetString("adt-ash-walker-ignite-hands-full"), ent, ent);
            return;
        }

        _popup.PopupEntity(Loc.GetString("adt-ash-walker-ignite-success"), ent, ent);
        args.Handled = true;
    }
}
