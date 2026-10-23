using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Trigger;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.ADT.Trigger;

public sealed class ReviveOnTriggerSystem : XOnTriggerSystem<ReviveOnTriggerComponent>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    protected override void OnTrigger(Entity<ReviveOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (_net.IsClient)
        {
            args.Handled = true;
            return;
        }

        _damageable.SetAllDamage(target, 0);
        _damageable.TryChangeDamage(target, ent.Comp.ResidualDamage, true);
        _bloodstream.TryModifyBloodLevel(target, ent.Comp.BloodLevel);
        _bloodstream.TryModifyBleedAmount(target, -ent.Comp.BleedReduction);

        if (TryComp<MobStateComponent>(target, out var mobState))
            _mobState.ChangeMobState(target, MobState.Alive, mobState);

        if (_mind.TryGetMind(target, out _, out var mind) && _player.TryGetSessionById(mind.UserId, out var session))
            _mind.UnVisit(session);

        if (ent.Comp.Vomit)
            _vomit.Vomit(target, -400f, -400f, force: true);

        args.Handled = true;
    }
}
