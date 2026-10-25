using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Mind.Components;
using Content.Shared.PowerCell;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Server.ADT.Silicons.Borgs;

public sealed class ADTNpcBorgSystem : EntitySystem
{
    [Dependency] private SharedBorgSystem _borg = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;

    private const float CheckInterval = 2f;
    private float _accumulator;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ADTNpcBorgComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTNpcBorgComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMapInit(Entity<ADTNpcBorgComponent> ent, ref MapInitEvent args)
    {
        Refresh(ent);
    }

    private void OnMindAdded(Entity<ADTNpcBorgComponent> ent, ref MindAddedMessage args)
    {
        if (TryComp<BorgChassisComponent>(ent, out var chassis) && chassis.Active)
            _powerCell.SetDrawEnabled(ent.Owner, true);

        RemCompDeferred<ADTNpcBorgComponent>(ent);
    }

    private void Refresh(Entity<ADTNpcBorgComponent> ent)
    {
        if (!TryComp<BorgChassisComponent>(ent, out var chassis))
            return;

        if (!chassis.Active)
            _borg.TryActivate((ent.Owner, chassis));

        if (chassis.Active && !ent.Comp.DrainPower)
            _powerCell.SetDrawEnabled(ent.Owner, false);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;

        if (_accumulator < CheckInterval)
            return;

        _accumulator = 0f;

        var query = EntityQueryEnumerator<ADTNpcBorgComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            Refresh((uid, comp));
        }
    }
}
