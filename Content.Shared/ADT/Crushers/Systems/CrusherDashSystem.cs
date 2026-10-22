using Content.Shared.Actions;
using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Crushers.Systems;

public sealed class CrusherDashSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly SoundSpecifier DashSound = new SoundPathSpecifier("/Audio/Effects/stealthoff.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrusherDashActiveComponent, ADTCrusherDashActionEvent>(OnDash);
        SubscribeLocalEvent<CrusherDashActiveComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CrusherDashActiveComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (now >= comp.ExpireTime)
                RemCompDeferred<CrusherDashActiveComponent>(uid);
        }
    }

    public void GrantDash(EntityUid user, TimeSpan duration)
    {
        var comp = EnsureComp<CrusherDashActiveComponent>(user);
        comp.ExpireTime = _timing.CurTime + duration;

        if (comp.ActionUid == null)
            comp.ActionUid = _actions.AddAction(user, comp.ActionProto);
    }

    private void OnDash(Entity<CrusherDashActiveComponent> ent, ref ADTCrusherDashActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var direction = _transform.ToMapCoordinates(args.Target).Position - _transform.GetMapCoordinates(ent).Position;

        if (direction.LengthSquared() < 0.01f)
            return;

        _throwing.TryThrow(ent, direction.Normalized() * args.Distance, args.Speed, null, 0, null, false, false, false);
        _audio.PlayPredicted(DashSound, ent, ent);
        _popup.PopupPredicted(
            Loc.GetString("adt-crusher-dash-self"),
            Loc.GetString("adt-crusher-dash-others", ("user", ent.Owner)),
            ent,
            ent);

        if (_net.IsServer)
            RemCompDeferred<CrusherDashActiveComponent>(ent);
    }

    private void OnShutdown(Entity<CrusherDashActiveComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ActionUid);
    }
}

public sealed partial class ADTCrusherDashActionEvent : WorldTargetActionEvent
{
    [DataField]
    public float Distance = 3f;

    [DataField]
    public float Speed = 10f;
}
