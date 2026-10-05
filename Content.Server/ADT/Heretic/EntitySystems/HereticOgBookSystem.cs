using Content.Shared.ADT.Heretic.Components.Side;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Heretic.EntitySystems;

public sealed partial class HereticOgBookSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HereticOgBookComponent, UseInHandEvent>(OnUse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HereticOgBookComponent>();
        while (query.MoveNext(out var uid, out var book))
        {
            if (book.Charges >= book.MaxCharges)
            {
                book.RechargeAccumulator = 0f;
                continue;
            }

            book.RechargeAccumulator += frameTime;
            if (book.RechargeAccumulator < book.RechargeTime)
                continue;

            book.RechargeAccumulator = 0f;
            book.Charges++;
            Dirty(uid, book);
        }
    }

    private void OnUse(Entity<HereticOgBookComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || ent.Comp.Charges <= 0)
        {
            if (!args.Handled)
                _popup.PopupEntity(Loc.GetString("heretic-ogbook-empty"), ent, ent);
            args.Handled = true;
            return;
        }

        args.Handled = true;
        ent.Comp.Charges--;
        Dirty(ent, ent.Comp);

        var coords = Transform(ent).Coordinates;
        Spawn("HereticOgBookBarrier", coords);
        _popup.PopupEntity(Loc.GetString("heretic-ogbook-used"), ent, ent);
    }
}