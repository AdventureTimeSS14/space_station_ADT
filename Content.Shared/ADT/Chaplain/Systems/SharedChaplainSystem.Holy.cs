using Content.Shared.Administration.Logs;
using Content.Shared.ADT.HolyDamage;
using Content.Shared.Bible.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Rejuvenate;

namespace Content.Shared.ADT.Chaplain.Systems;

public abstract partial class SharedChaplainSystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    private void InitializeHoly()
    {
        SubscribeLocalEvent<HolyAffectedComponent, DamageDealtEvent>(OnHolyAffectedDamageDealt);
        SubscribeLocalEvent<HolySuppressionComponent, RejuvenateEvent>(OnSuppressionRejuvenate);
    }

    private void OnHolyAffectedDamageDealt(Entity<HolyAffectedComponent> ent, ref DamageDealtEvent args)
    {
        if (!ent.Comp.Suppressible || args.Origin is not { } origin || !HasComp<BibleUserComponent>(origin))
            return;

        if (!TryComp<HolyDamageComponent>(_hands.GetActiveItem(origin), out var weapon))
            return;

        var suppression = EnsureComp<HolySuppressionComponent>(ent);
        var amount = suppression.Amount > 0
            ? ent.Comp.SuppressionHit ?? weapon.SuppressionHit
            : ent.Comp.SuppressionFirstHit ?? weapon.SuppressionFirstHit;

        if (ent.Comp.SuppressionRecovery is { } recovery)
            suppression.Recovery = recovery;

        if (suppression.Amount <= 0)
            suppression.NextRecovery = _timing.CurTime + suppression.RecoveryInterval;

        suppression.Amount = FixedPoint2.Min(suppression.Amount + amount, suppression.Max);
        Dirty(ent, suppression);

        _adminLog.Add(LogType.Damaged, LogImpact.Low,
            $"{ToPrettyString(ent):target} took {amount} holy suppression from {ToPrettyString(origin):attacker}");
    }

    private void OnSuppressionRejuvenate(Entity<HolySuppressionComponent> ent, ref RejuvenateEvent args)
    {
        RemCompDeferred<HolySuppressionComponent>(ent);
    }

    private void UpdateSuppression(TimeSpan now)
    {
        var query = EntityQueryEnumerator<HolySuppressionComponent>();
        while (query.MoveNext(out var uid, out var suppression))
        {
            if (now < suppression.NextRecovery)
                continue;

            suppression.NextRecovery = now + suppression.RecoveryInterval;
            suppression.Amount = FixedPoint2.Max(suppression.Amount - suppression.Recovery, FixedPoint2.Zero);

            if (suppression.Amount <= 0)
            {
                RemCompDeferred<HolySuppressionComponent>(uid);
                continue;
            }

            Dirty(uid, suppression);
        }
    }

    public FixedPoint2? GetSuppression(EntityUid uid)
    {
        return CompOrNull<HolySuppressionComponent>(uid)?.Amount;
    }

    public void SetHolyAffected(Entity<HolyAffectedComponent?> ent,
        bool piercing,
        bool bibleSmite,
        bool suppressible,
        FixedPoint2? suppressionFirstHit = null,
        FixedPoint2? suppressionHit = null,
        FixedPoint2? suppressionRecovery = null)
    {
        ent.Comp ??= EnsureComp<HolyAffectedComponent>(ent);

        ent.Comp.Piercing = piercing;
        ent.Comp.BibleSmite = bibleSmite;
        ent.Comp.Suppressible = suppressible;
        ent.Comp.SuppressionFirstHit = suppressionFirstHit;
        ent.Comp.SuppressionHit = suppressionHit;
        ent.Comp.SuppressionRecovery = suppressionRecovery;
        Dirty(ent);
    }

    public bool CanBibleSmite(EntityUid target)
    {
        return TryComp<HolyAffectedComponent>(target, out var holy) && holy.BibleSmite;
    }
}
