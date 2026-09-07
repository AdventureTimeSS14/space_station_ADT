using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Robust.Shared.Player;

namespace Content.Server.ADT.Mind;

public sealed class ReturnToBodyOnReviveSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ReturnToBodyPromptEvent>(OnReturnToBodyPrompt);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.OldMobState != MobState.Dead || ev.NewMobState != MobState.Alive)
            return;

        QueueLocalEvent(new ReturnToBodyPromptEvent(ev.Target));
    }

    private void OnReturnToBodyPrompt(ReturnToBodyPromptEvent ev)
    {
        var uid = ev.Target;

        if (Deleted(uid) ||
            !_mind.TryGetMind(uid, out var mindUid, out var mindComp) ||
            !_player.TryGetSessionById(mindComp.UserId, out var playerSession) ||
            mindComp.CurrentEntity == uid)
        {
            return;
        }

        _eui.OpenEui(new ReturnToBodyEui(mindComp, _mind, _player), playerSession);
    }
}

public sealed class ReturnToBodyPromptEvent : EntityEventArgs
{
    public EntityUid Target { get; }

    public ReturnToBodyPromptEvent(EntityUid target)
    {
        Target = target;
    }
}
