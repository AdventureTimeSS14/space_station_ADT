// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.NullRod.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Timing;
using Content.Shared.Damage.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.Bible.Components;

namespace Content.Server.NullRod;

public sealed partial class NullDamageSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnholyComponent, DamageDealtEvent>(OnUnholyDamageTaken);
        SubscribeLocalEvent<NullDamageComponent, RejuvenateEvent>(OnRejuvenate);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateNullDamage();
    }

    private void UpdateNullDamage()
    {
        var currentTime = _timing.CurTime;

        var query = EntityQueryEnumerator<NullDamageComponent>();
        while (query.MoveNext(out var uid, out var nullDamage))
        {
            if (nullDamage.NullDamage <= 0)
            {
                RemComp<NullDamageComponent>(uid);
                continue;
            }

            if (currentTime >= nullDamage.NextNullDamageTick)
            {
                RecoverNullDamage(nullDamage, nullDamage.NullDamageRecoveryPerTick);

                nullDamage.NextNullDamageTick = currentTime + TimeSpan.FromSeconds(nullDamage.NullDamageRecoveryInterval);
                Dirty(uid, nullDamage);
            }
        }
    }

    #region Null Damage Logic

    private void OnUnholyDamageTaken(EntityUid uid, UnholyComponent component, ref DamageDealtEvent args)
    {
        if (!args.Origin.HasValue || !HasComp<BibleUserComponent>(args.Origin.Value))
            return;

        var heldEntity = _hands.GetActiveItem(args.Origin.Value);
        if (!TryComp<NullRodComponent>(heldEntity, out var nullRodComp))
            return;

        var nullDamage = EnsureComp<NullDamageComponent>(uid);
        var damageToApply = nullDamage.NullDamage > 0
            ? nullRodComp.NullDamage
            : nullRodComp.FirstNullDamage;

        AddNullDamage(nullDamage, damageToApply);

        if (nullDamage.NextNullDamageTick == default)
        {
            nullDamage.NextNullDamageTick = _timing.CurTime + TimeSpan.FromSeconds(nullDamage.NullDamageRecoveryInterval);
        }

        Dirty(uid, nullDamage);

        _admin.Add(LogType.Damaged, LogImpact.Low,
            $"{ToPrettyString(uid):target} took {damageToApply} NullDamage from {ToPrettyString(args.Origin.Value):attacker}");
    }

    private void OnRejuvenate(EntityUid uid, NullDamageComponent component, ref RejuvenateEvent args)
        => RemCompDeferred<NullDamageComponent>(uid);

    #endregion

    #region Public API

    public void AddNullDamage(NullDamageComponent nullDamage, FixedPoint2 amount)
    {
        nullDamage.NullDamage = FixedPoint2.Min(nullDamage.NullDamage + amount, nullDamage.MaxNullDamage);
    }

    public void RecoverNullDamage(NullDamageComponent nullDamage, FixedPoint2 amount)
    {
        nullDamage.NullDamage = FixedPoint2.Max(nullDamage.NullDamage - amount, FixedPoint2.Zero);
    }

    public FixedPoint2? GetNullDamage(EntityUid uid, NullDamageComponent? nullDamage = null)
    {
        if (!Resolve(uid, ref nullDamage, false))
            return null;

        return nullDamage.NullDamage;
    }

    #endregion
}
