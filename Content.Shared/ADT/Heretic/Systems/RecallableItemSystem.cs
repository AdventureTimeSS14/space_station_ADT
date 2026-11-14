//

using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Components;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Projectiles;
using Content.Shared.Whitelist;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared.ADT.Heretic.Systems;

public sealed partial class RecallableItemSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RecallableItemComponent, GotEquippedHandEvent>(OnEquipped);
        SubscribeLocalEvent<RecallItemEvent>(OnRecall);

        _player.PlayerStatusChanged += StatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _player.PlayerStatusChanged -= StatusChanged;
    }

    private void StatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.InGame)
            return;

        var query = EntityQueryEnumerator<RecallableItemComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.User is { } user && user == e.Session.AttachedEntity)
                _pvsOverride.AddSessionOverride(uid, e.Session);
        }
    }

    private void OnEquipped(Entity<RecallableItemComponent> ent, ref GotEquippedHandEvent args)
    {
        if (args.User == ent.Comp.User || !ShouldAddAction(ent.Comp, args.User))
            return;

        if (ent.Comp.User is { } oldUser && _player.TryGetSessionByEntity(oldUser, out var oldSession))
            _pvsOverride.RemoveSessionOverride(ent, oldSession);

        _player.TryGetSessionByEntity(args.User, out var session);

        foreach (var act in _actions.GetActions(args.User))
        {
            if (Prototype(act)?.ID is not { } id || id != ent.Comp.ActionId)
                continue;

            _actions.RemoveAction(args.User, act.AsNullable());

            if (act.Comp.Container is not { } container || session == null)
                continue;

            _pvsOverride.RemoveSessionOverride(container, session);
        }

        _actions.AddAction(args.User, ref ent.Comp.Action, ent.Comp.ActionId, ent);
        ent.Comp.User = args.User;

        if (session != null)
            _pvsOverride.AddSessionOverride(ent, session);
    }

    private bool ShouldAddAction(RecallableItemComponent comp, EntityUid user)
    {
        var result = _whitelist.CheckBoth(user, blacklist: comp.UserBlacklist, whitelist: comp.UserWhitelist);
        if (result || !comp.WhitelistCheckMind)
            return result;

        return _mind.TryGetMind(user, out var mind, out _) &&
               _whitelist.CheckBoth(mind, blacklist: comp.UserBlacklist, whitelist: comp.UserWhitelist);
    }

    private void OnRecall(RecallItemEvent args)
    {
        if (!Exists(args.Action.Comp.Container))
            return;

        var user = args.Performer;

        if (!TryComp(user, out HandsComponent? hands))
            return;

        var item = args.Action.Comp.Container.Value;

        if (!TryComp(item, out ItemComponent? itemComp) || _hands.IsHolding((user, hands), item) ||
            !_hands.TryGetEmptyHand((user, hands), out var hand))
            return;

        args.Handled = true;

        if (TryComp(item, out EmbeddableProjectileComponent? embeddable) && embeddable.EmbeddedIntoUid != null)
            _projectile.EmbedDetach(item, embeddable);

        _transform.AttachToGridOrMap(item);
        _transform.SetCoordinates(item, Transform(user).Coordinates);
        _hands.TryPickup(user, item, hand, false, handsComp: hands, item: itemComp);
    }
}
