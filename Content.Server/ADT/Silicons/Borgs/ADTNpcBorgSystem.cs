using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.PowerCell;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Server.ADT.Silicons.Borgs;

public sealed class ADTNpcBorgSystem : EntitySystem
{
    [Dependency] private SharedBorgSystem _borg = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTNpcBorgComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTNpcBorgComponent, MobStateChangedEvent>(OnMobStateChanged, after: [typeof(SharedBorgSystem)]);
        SubscribeLocalEvent<ADTNpcBorgComponent, PowerCellChangedEvent>(OnPowerCellChanged, after: [typeof(SharedBorgSystem)]);
        SubscribeLocalEvent<ADTNpcBorgComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMapInit(Entity<ADTNpcBorgComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<BorgChassisComponent>(ent, out var chassis))
            _borg.TryActivate((ent.Owner, chassis));

        DisableDraw(ent);
    }

    private void OnMobStateChanged(Entity<ADTNpcBorgComponent> ent, ref MobStateChangedEvent args)
    {
        DisableDraw(ent);
    }

    private void OnPowerCellChanged(Entity<ADTNpcBorgComponent> ent, ref PowerCellChangedEvent args)
    {
        DisableDraw(ent);
    }

    private void OnMindAdded(Entity<ADTNpcBorgComponent> ent, ref MindAddedMessage args)
    {
        if (TryComp<BorgChassisComponent>(ent, out var chassis) && chassis.Active)
            _powerCell.SetDrawEnabled(ent.Owner, true);

        RemCompDeferred<ADTNpcBorgComponent>(ent);
    }

    private void DisableDraw(EntityUid uid)
    {
        if (TryComp<BorgChassisComponent>(uid, out var chassis) && chassis.Active)
            _powerCell.SetDrawEnabled(uid, false);
    }
}
