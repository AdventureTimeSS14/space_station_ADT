using Content.Shared.ADT.RoleIntro;
using Content.Shared.Mind.Components;
using Robust.Server.Player;

namespace Content.Server.ADT.RoleIntro;

public sealed class ADTRoleIntroSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTRoleIntroComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(Entity<ADTRoleIntroComponent> ent, ref MindAddedMessage args)
    {
        if (args.Mind.Comp.UserId is not { } userId)
            return;

        if (!_player.TryGetSessionById(userId, out var session))
            return;

        var ev = new ADTRoleIntroEvent(ent.Comp.Title, ent.Comp.Text, ent.Comp.LockTime);
        RaiseNetworkEvent(ev, session);
    }
}
