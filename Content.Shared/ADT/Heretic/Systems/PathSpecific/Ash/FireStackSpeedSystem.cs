//

using Content.Shared.Atmos.Components;
using Content.Shared.Heretic.Components.PathSpecific.Ash;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Ash;

public sealed partial class FireStackSpeedSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _modifier = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(0.5);
    private TimeSpan _nextRefresh = TimeSpan.Zero;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextRefresh)
            return;

        _nextRefresh = _timing.CurTime + RefreshInterval;

        var query = EntityQueryEnumerator<FireStackSpeedComponent, FlammableComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            _modifier.RefreshMovementSpeedModifiers(uid);
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireStackSpeedComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
    }

    private void OnRefresh(Entity<FireStackSpeedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp(ent, out FlammableComponent? flam) || !flam.OnFire || flam.FireStacks <= 0f)
            return;

        args.ModifySpeed(1f + flam.FireStacks * ent.Comp.FireStackSpeedMultiplier);
    }
}
