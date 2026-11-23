using Content.Shared.Mobs.Systems;
using Content.Shared.Pulling.Events;

namespace Content.Shared.ADT.Lavaland.Goliath;

public sealed class ADTLavalandGoliathPullSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLavalandGoliathComponent, BeingPulledAttemptEvent>(OnBeingPulledAttempt);
    }

    private void OnBeingPulledAttempt(Entity<ADTLavalandGoliathComponent> ent, ref BeingPulledAttemptEvent args)
    {
        if (!_mobState.IsDead(ent))
            args.Cancel();
    }
}
