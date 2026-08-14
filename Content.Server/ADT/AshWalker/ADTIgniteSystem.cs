using Content.Shared.ADT.AshWalker;
using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IgnitionSource.Components;
using Content.Shared.IgnitionSource.EntitySystems;
using Content.Shared.Interaction.Events;
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
        SubscribeLocalEvent<ADTSmallBlazeComponent, DroppedEvent>(OnBlazeDropped);
    }

    private void OnBlazeDropped(Entity<ADTSmallBlazeComponent> ent, ref DroppedEvent args)
    {
        QueueDel(ent.Owner);
    }

    private void OnMapInit(Entity<ADTIgniteComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.ActionId);
    }

    private void OnShutdown(Entity<ADTIgniteComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ActionEntity);
    }

    private void OnIgnite(Entity<ADTIgniteComponent> ent, ref ADTIgniteActionEvent args)
    {
        if (args.Handled)
            return;

        var attempt = new IngestionAttemptEvent(MouthSlots);
        RaiseLocalEvent(ent.Owner, ref attempt);

        if (attempt.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("adt-ash-walker-ignite-maw-covered"), ent.Owner, ent.Owner);
            return;
        }

        var blaze = SpawnAtPosition(ent.Comp.Blaze, Transform(ent.Owner).Coordinates);

        if (TryComp<MatchstickComponent>(blaze, out var matchstick))
            _matchstick.TryIgnite((blaze, matchstick), ent.Owner);

        if (!_hands.TryPickupAnyHand(ent.Owner, blaze))
        {
            QueueDel(blaze);
            _popup.PopupEntity(Loc.GetString("adt-ash-walker-ignite-hands-full"), ent.Owner, ent.Owner);
            return;
        }

        _popup.PopupEntity(Loc.GetString("adt-ash-walker-ignite-success"), ent.Owner, ent.Owner);
        args.Handled = true;
    }
}
