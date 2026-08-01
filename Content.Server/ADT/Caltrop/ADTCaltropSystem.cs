using Content.Shared.ADT.Caltrop;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Caltrop;

public sealed class ADTCaltropSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTCaltropComponent, StepTriggerAttemptEvent>(OnStepAttempt);
        SubscribeLocalEvent<ADTCaltropComponent, StepTriggeredOffEvent>(OnStepped);
    }

    private void OnStepAttempt(Entity<ADTCaltropComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue |= HasComp<MobStateComponent>(args.Tripper);
    }

    private void OnStepped(Entity<ADTCaltropComponent> ent, ref StepTriggeredOffEvent args)
    {
        var tripper = args.Tripper;

        if (!HasComp<MobStateComponent>(tripper) || !_random.Prob(ent.Comp.Probability))
            return;

        var damage = ent.Comp.Damage * _random.NextFloat(ent.Comp.MinMultiplier, ent.Comp.MaxMultiplier);
        var dealt = _damageable.ChangeDamage(tripper, damage, origin: ent.Owner);

        if (!dealt.AnyPositive())
            return;

        if (ent.Comp.ParalyzeTime > TimeSpan.Zero)
            _stun.TryUpdateParalyzeDuration(tripper, ent.Comp.ParalyzeTime);

        _audio.PlayPvs(ent.Comp.Sound, tripper);

        _adminLogger.Add(LogType.Damaged, LogImpact.Low,
            $"{ToPrettyString(tripper):user} stepped on {ToPrettyString(ent.Owner):target} and took {dealt.GetTotal():damage} damage");

        if (_timing.CurTime < ent.Comp.NextMessage)
            return;

        ent.Comp.NextMessage = _timing.CurTime + ent.Comp.MessageCooldown;

        var self = Loc.GetString(ent.Comp.PopupSelf, ("target", ent.Owner));
        var others = Loc.GetString(ent.Comp.PopupOthers,
            ("user", Identity.Entity(tripper, EntityManager)),
            ("target", ent.Owner));

        _popup.PopupEntity(self, tripper, tripper, PopupType.MediumCaution);
        _popup.PopupEntity(others, tripper, Filter.PvsExcept(tripper), true, PopupType.MediumCaution);
    }
}
