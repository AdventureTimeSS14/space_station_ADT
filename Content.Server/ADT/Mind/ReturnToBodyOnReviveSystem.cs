using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Shared.ADT.Silicon.Components;
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

        SubscribeLocalEvent<SiliconComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<SiliconComponent> ent, ref MobStateChangedEvent ev)
    {
        if (ev.OldMobState != MobState.Dead || ev.NewMobState != MobState.Alive)
            return;

        var uid = ent.Owner;

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
