using Content.Server.ADT.Bubblegum.Abilities;
using Content.Server.ADT.Salvage.Systems;
using Content.Server.NPC.HTN;
using Content.Shared.ADT.Bubblegum;
using Content.Shared.ADT.Bubblegum.Abilities;
using Content.Shared.ADT.Salvage.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.ADT.Bubblegum;

public sealed class BubblegumCombatSystem : EntitySystem
{
    [Dependency] private ADTMegafaunaChargeSystem _charge = default!;
    [Dependency] private BubblegumSystem _bubblegum = default!;
    [Dependency] private MegafaunaSystem _megafauna = default!;
    [Dependency] private BubblegumBloodWarpSystem _warp = default!;
    [Dependency] private BubblegumHallucinationChargeSystem _hallucination = default!;
    [Dependency] private BubblegumSummonNarsiSystem _narsi = default!;
    [Dependency] private BubblegumSurroundSystem _surround = default!;
    [Dependency] private BubblegumTripleChargeSystem _tripleCharge = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumPendingWavesComponent, ComponentStartup>(OnWavesStartup);
        SubscribeLocalEvent<BubblegumPendingWavesComponent, ComponentShutdown>(OnWavesShutdown);
        SubscribeLocalEvent<BubblegumPendingWavesComponent, ADTMegafaunaBusyCheckEvent>(OnWavesBusyCheck);

        SubscribeLocalEvent<BubblegumComponent, ADTMegafaunaChargeTrampleAttemptEvent>(OnTrampleAttempt);
        SubscribeLocalEvent<BubblegumHallucinationComponent, ADTMegafaunaChargeTrampleAttemptEvent>(OnTrampleAttempt);
        SubscribeLocalEvent<BubblegumMinionComponent, ADTMegafaunaChargeTrampleAttemptEvent>(OnTrampleAttempt);
        SubscribeLocalEvent<BubblegumHallucinationComponent, ADTMegafaunaChargeEndedEvent>(OnHallucinationChargeEnded);

        SubscribeLocalEvent<BubblegumComponent, ADTMegafaunaAttemptOpenFireEvent>(OnAttemptOpenFire);
        SubscribeLocalEvent<BubblegumComponent, ADTMegafaunaOpenFireEvent>(OnOpenFire);
    }

    private void OnWavesStartup(Entity<BubblegumPendingWavesComponent> ent, ref ComponentStartup args)
    {
        _megafauna.RefreshBusy(ent);
    }

    private void OnWavesShutdown(Entity<BubblegumPendingWavesComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent))
            _megafauna.RefreshBusy(ent);
    }

    private void OnWavesBusyCheck(Entity<BubblegumPendingWavesComponent> ent, ref ADTMegafaunaBusyCheckEvent args)
    {
        if (ent.Comp.LifeStage < ComponentLifeStage.Stopping)
            args.Busy = true;
    }

    private void OnTrampleAttempt<T>(Entity<T> ent, ref ADTMegafaunaChargeTrampleAttemptEvent args) where T : IComponent
    {
        args.Cancelled = true;
    }

    private void OnHallucinationChargeEnded(Entity<BubblegumHallucinationComponent> ent, ref ADTMegafaunaChargeEndedEvent args)
    {
        QueueDel(ent);
    }

    private void OnAttemptOpenFire(Entity<BubblegumComponent> ent, ref ADTMegafaunaAttemptOpenFireEvent args)
    {
        if (IsBusy(ent))
            args.Cancelled = true;
    }

    private void OnOpenFire(Entity<BubblegumComponent> ent, ref ADTMegafaunaOpenFireEvent args)
    {
        if (TryComp<ADTMegafaunaAIComponent>(ent, out var ai))
            _megafauna.SetRangedCooldown((ent.Owner, ai), ent.Comp.AbilityInterval);

        Decide(ent, args.Target);
    }

    private bool IsBusy(EntityUid uid)
    {
        return _charge.IsCharging(uid)
               || HasComp<BubblegumPendingWavesComponent>(uid)
               || HasComp<BubblegumPendingWarpComponent>(uid);
    }

    private void Decide(Entity<BubblegumComponent> ent, EntityUid target)
    {
        var targetCoords = _transform.GetMapCoordinates(target);
        if (targetCoords.MapId == MapId.Nullspace)
            return;

        // anger_modifier (0..20)
        var anger = ent.Comp.AngerModifier;

        // if(!try_bloodattack() || prob(25 + anger)) blood_warp()
        if (_random.Prob(Math.Clamp(0.25f + anger / 100f, 0f, 1f))
            && TryComp<BubblegumBloodWarpComponent>(ent, out var warp)
            && _warp.TryBloodWarp((ent.Owner, warp), targetCoords))
        {
            _megafauna.Say(ent, "bubblegum-blood-warp", 3);
            return;
        }

        // if(!BUBBLEGUM_SMASH) triple_charge()
        if (!ent.Comp.InSmashPhase)
        {
            if (TryComp<BubblegumTripleChargeComponent>(ent, out var tc))
            {
                _megafauna.Say(ent, "bubblegum-triple-charge", 3);
                _tripleCharge.StartNpcTripleCharge((ent.Owner, tc), targetCoords);
            }
            return;
        }

        // prob(25) && enraged → hit_up_narsi()
        if (_bubblegum.IsEnraged(ent) && _random.Prob(0.25f))
        {
            if (TryComp<BubblegumSummonNarsiComponent>(ent, out var narsi))
            {
                _megafauna.Say(ent, "bubblegum-narsi", 3);
                _narsi.TrySummon((ent.Owner, narsi), targetCoords);
            }
            return;
        }

        if (_random.Prob(Math.Clamp(0.5f + anger / 100f, 0f, 1f)))
        {
            if (TryComp<BubblegumHallucinationChargeComponent>(ent, out var hc))
            {
                _megafauna.Say(ent, "bubblegum-hallucination", 3);
                _hallucination.StartHallucinationCharge((ent.Owner, hc), targetCoords, target);
            }
            return;
        }

        if (TryComp<BubblegumSurroundComponent>(ent, out var sc))
        {
            _megafauna.Say(ent, "bubblegum-surround", 3);
            _surround.StartSurround((ent.Owner, sc), targetCoords, target);
        }
    }

}
