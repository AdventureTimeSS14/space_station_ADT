using Content.Server.Body.Systems;
using Content.Shared.ADT.Implants.SecondHeartImplant;
using Content.Shared.Damage.Systems;
using Content.Shared.Implants;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Implants.SecondHeartImplant;

public sealed class SecondHeartImplantSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float VisionDuration = 2.5f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SecondHeartImplantComponent, ActivateSecondHeartImplantActionEvent>(OnActivate);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<SecondHeartVisionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.EndTime > curTime)
                continue;

            RemCompDeferred<SecondHeartVisionComponent>(uid);
        }
    }

    private void OnActivate(Entity<SecondHeartImplantComponent> ent, ref ActivateSecondHeartImplantActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;

        if (!_mobState.IsCritical(user) && !_mobState.IsDead(user))
        {
            _popup.PopupEntity(Loc.GetString("second-heart-implant-not-crit"), user, user, PopupType.SmallCaution);
            return;
        }

        args.Handled = true;

        _damageable.SetAllDamage(user, 0);
        _damageable.TryChangeDamage(user, ent.Comp.ResidualDamage, true);
        _bloodstream.TryModifyBloodLevel(user, 400f);
        _bloodstream.TryModifyBleedAmount(user, -100f);

        if (TryComp<MobStateComponent>(user, out var mobStateComp))
            _mobState.ChangeMobState(user, MobState.Alive, mobStateComp);

        if (_mind.TryGetMind(user, out _, out var mindComp) &&
            _player.TryGetSessionById(mindComp.UserId, out var session))
        {
            _mind.UnVisit(session);
        }

        _audio.PlayPvs(ent.Comp.Sound, user);
        _popup.PopupEntity(Loc.GetString("second-heart-implant-activate"), user, PopupType.Large);

        var vision = EnsureComp<SecondHeartVisionComponent>(user);
        vision.StartTime = _timing.CurTime;
        vision.EndTime = _timing.CurTime + TimeSpan.FromSeconds(VisionDuration);

        _vomit.Vomit(user, -400f, -400f, force: true);

        _implants.ForceRemove(user, ent.Owner);
    }
}
