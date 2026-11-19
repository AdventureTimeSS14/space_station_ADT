using Content.Shared.Administration.Logs;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.ADT.Heretic.Common;
using Content.Shared.ADT.HolyDamage;
using Content.Shared.Bible.Components;
using Content.Shared.Damage;
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

    private const string HolyDamageType = "Holy";
    private const string HolyBurnDamageType = "Heat";

    private void InitializeHoly()
    {
        SubscribeLocalEvent<HolyAffectedComponent, DamageDealtEvent>(OnHolyAffectedDamageDealt);
        SubscribeLocalEvent<HolySuppressionComponent, RejuvenateEvent>(OnSuppressionRejuvenate);
        SubscribeLocalEvent<HolyShieldComponent, BeforeCastTouchSpellEvent>(OnShieldTouchSpell);
    }

    public float GetHolyVulnerability(EntityUid target)
    {
        if (!TryComp<HolyAffectedComponent>(target, out var holy) || holy.Power < holy.MinPower)
            return 0f;

        var vulnerability = holy.BaseVulnerability + (holy.Power - holy.MinPower) * holy.VulnerabilityPerPower;
        return MathF.Min(vulnerability, holy.MaxVulnerability);
    }

    public void SetHolyPower(EntityUid target, float power)
    {
        if (!TryComp<HolyAffectedComponent>(target, out var holy) || holy.Power == power)
            return;

        holy.Power = power;
        Dirty(target, holy);
    }

    public bool TryDealHolyDamage(EntityUid target, DamageSpecifier damage, EntityUid? origin)
    {
        return damage.DamageDict.TryGetValue(HolyDamageType, out var holy) && TryDealHolyDamage(target, holy, origin);
    }

    public bool TryDealHolyDamage(EntityUid target, FixedPoint2 holy, EntityUid? origin)
    {
        if (holy <= FixedPoint2.Zero)
            return false;

        var multiplier = GetHolyVulnerability(target) * GetHolyMultiplier(origin);
        if (multiplier <= 0f)
            return false;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add(HolyBurnDamageType, holy * multiplier);
        return _damageable.TryChangeDamage(target, damage, ignoreResistances: true, origin: origin);
    }

    private void OnShieldTouchSpell(Entity<HolyShieldComponent> ent, ref BeforeCastTouchSpellEvent args)
    {
        args.Cancel();
    }

    private void OnHolyAffectedDamageDealt(Entity<HolyAffectedComponent> ent, ref DamageDealtEvent args)
    {
        if (!ent.Comp.Suppressible || args.Origin is not { } origin || !HasComp<BibleUserComponent>(origin))
            return;

        if (!TryComp<HolyDamageComponent>(_hands.GetActiveItem(origin), out var weapon))
            return;

        var suppressed = TryComp<HolySuppressionComponent>(ent, out var current) && current.Amount > 0;
        var amount = suppressed
            ? ent.Comp.SuppressionHit ?? weapon.SuppressionHit
            : ent.Comp.SuppressionFirstHit ?? weapon.SuppressionFirstHit;

        amount *= GetHolyMultiplier(origin);
        AddSuppression(ent, amount);

        _adminLog.Add(LogType.Damaged, LogImpact.Low,
            $"{ToPrettyString(ent):target} took {amount} holy suppression from {ToPrettyString(origin):attacker}");
    }

    public void AddSuppression(Entity<HolyAffectedComponent> ent, FixedPoint2 amount)
    {
        if (amount <= 0)
            return;

        var suppression = EnsureComp<HolySuppressionComponent>(ent);

        if (ent.Comp.SuppressionRecovery is { } recovery)
            suppression.Recovery = recovery;

        if (suppression.Amount <= 0)
            suppression.NextRecovery = _timing.CurTime + suppression.RecoveryInterval;

        suppression.Amount = FixedPoint2.Min(suppression.Amount + amount, suppression.Max);
        Dirty(ent, suppression);
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
