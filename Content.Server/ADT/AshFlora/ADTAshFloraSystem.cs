using Content.Shared.ADT.AshFlora;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.AshFlora;

public sealed class ADTAshFloraSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTAshFloraComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<ADTAshFloraComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ADTAshFloraComponent, ADTAshFloraHarvestDoAfterEvent>(OnHarvestDoAfter);

        SubscribeLocalEvent<ADTAshFloraRegrowthComponent, MapInitEvent>(OnRegrowthMapInit);
    }

    private void OnInteractHand(Entity<ADTAshFloraComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || ent.Comp.ToolWhitelist != null)
            return;

        args.Handled = TryStartHarvest(ent, args.User, null);
    }

    private void OnInteractUsing(Entity<ADTAshFloraComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || ent.Comp.ToolWhitelist == null)
            return;

        if (_whitelist.IsWhitelistFail(ent.Comp.ToolWhitelist, args.Used))
            return;

        args.Handled = TryStartHarvest(ent, args.User, args.Used);
    }

    private bool TryStartHarvest(Entity<ADTAshFloraComponent> ent, EntityUid user, EntityUid? used)
    {
        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.HarvestTime, new ADTAshFloraHarvestDoAfterEvent(), ent.Owner, target: ent.Owner, used: used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return false;

        _popup.PopupEntity(Loc.GetString(ent.Comp.PopupStart, ("target", ent.Owner)), ent, user);
        return true;
    }

    private void OnHarvestDoAfter(Entity<ADTAshFloraComponent> ent, ref ADTAshFloraHarvestDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        Harvest(ent, args.User);
    }

    private void Harvest(Entity<ADTAshFloraComponent> ent, EntityUid user)
    {
        var amount = _random.Next(ent.Comp.MinAmount, ent.Comp.MaxAmount + 1);

        if (ent.Comp.Product is { } product)
        {
            var coords = _transform.GetMapCoordinates(ent.Owner);

            for (var i = 0; i < amount; i++)
            {
                Spawn(product, coords.Offset(_random.NextVector2(ent.Comp.ScatterOffset)));
            }
        }

        var message = ent.Comp.PopupMedium;

        if (amount <= ent.Comp.MinAmount)
            message = ent.Comp.PopupLow;
        else if (amount >= ent.Comp.MaxAmount)
            message = ent.Comp.PopupHigh;

        _popup.PopupEntity(Loc.GetString(message), ent, user);

        if (ent.Comp.HarvestedPrototype is { } harvested)
            Spawn(harvested, Transform(ent).Coordinates);

        QueueDel(ent);
    }

    private void OnRegrowthMapInit(Entity<ADTAshFloraRegrowthComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.RegrowAt = _timing.CurTime + _random.Next(ent.Comp.MinTime, ent.Comp.MaxTime);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTAshFloraRegrowthComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.RegrowAt)
                continue;

            Spawn(comp.Prototype, Transform(uid).Coordinates);
            QueueDel(uid);
        }
    }
}
