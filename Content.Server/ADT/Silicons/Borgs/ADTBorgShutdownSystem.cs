using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Silicons.Borgs;

public sealed class ADTBorgShutdownSystem : EntitySystem
{
    [Dependency] private readonly SharedBorgSystem _borg = default!;
    [Dependency] private readonly SharedHandheldLightSystem _handheldLight = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTBorgShutdownComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ADTBorgShutdownComponent, ComponentShutdown>(OnShutdown);
    }

    public bool TryShutdown(EntityUid uid, TimeSpan duration)
    {
        if (!HasComp<BorgChassisComponent>(uid))
            return false;

        var comp = EnsureComp<ADTBorgShutdownComponent>(uid);
        var end = _timing.CurTime + duration;

        if (comp.EndTime < end)
        {
            comp.EndTime = end;
            Dirty(uid, comp);
        }

        return true;
    }

    private void OnStartup(Entity<ADTBorgShutdownComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<BorgChassisComponent>(ent, out var chassis))
            _borg.SetActive((ent.Owner, chassis), false);

        if (TryComp<HandheldLightComponent>(ent, out var light) && light.Activated)
            _handheldLight.TurnOff((ent.Owner, light), false);
    }

    private void OnShutdown(Entity<ADTBorgShutdownComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (TryComp<BorgChassisComponent>(ent, out var chassis))
            _borg.TryActivate((ent.Owner, chassis));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTBorgShutdownComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.EndTime)
                continue;

            RemCompDeferred<ADTBorgShutdownComponent>(uid);
        }
    }
}
