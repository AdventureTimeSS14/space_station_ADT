using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Backstab;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Crushers.Systems;

public sealed class CrusherRageSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private BackstabDamageMultipilierSystem _backstab = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTCrusherRageOnDetonateComponent, CrusherMarkDetonatedEvent>(OnDetonated);
        SubscribeLocalEvent<ADTCrusherRageOnDetonateComponent, TrophyHolderExamineEvent>(OnTrophyExamine);
        SubscribeLocalEvent<ADTCrusherRageStatusEffectComponent, StatusEffectRemovedEvent>(OnRageRemoved);
    }

    private void OnDetonated(Entity<ADTCrusherRageOnDetonateComponent> ent, ref CrusherMarkDetonatedEvent args)
    {
        var now = _timing.CurTime;

        if (_status.HasStatusEffect(args.User, ent.Comp.RageEffect))
        {
            Leech(ent, args.User);

            if (_backstab.IsBehindTarget(args.User, args.Target))
                ExtendRage(ent, args.User, now);

            return;
        }

        if (_status.HasStatusEffect(args.User, ent.Comp.ExhaustionEffect))
            return;

        if (now - ent.Comp.LastStack > ent.Comp.StackDecay)
            ent.Comp.Stacks = 0;

        ent.Comp.LastStack = now;
        ent.Comp.Stacks++;

        if (ent.Comp.Stacks < ent.Comp.MaxStacks)
        {
            _popup.PopupEntity(Loc.GetString("adt-crusher-rage-stack-popup",
                    ("stacks", ent.Comp.Stacks),
                    ("max", ent.Comp.MaxStacks)),
                args.User,
                args.User);
            return;
        }

        ent.Comp.Stacks = 0;

        if (!_status.TrySetStatusEffectDuration(args.User, ent.Comp.RageEffect, ent.Comp.RageDuration))
            return;

        ent.Comp.RageStart = now;
        _audio.PlayPvs(ent.Comp.RageSound, args.User);
        _popup.PopupEntity(Loc.GetString("adt-crusher-rage-start-popup"), args.User, PopupType.LargeCaution);
    }

    private void ExtendRage(Entity<ADTCrusherRageOnDetonateComponent> ent, EntityUid user, TimeSpan now)
    {
        if (!_status.TryGetTime(user, ent.Comp.RageEffect, out var time) || time.EndEffectTime is not { } end)
            return;

        var limit = ent.Comp.RageStart + ent.Comp.MaxRageDuration;
        var newEnd = end + ent.Comp.BackstabExtension;

        if (newEnd > limit)
            newEnd = limit;

        if (newEnd <= end)
            return;

        _status.TrySetStatusEffectDuration(user, ent.Comp.RageEffect, newEnd - now);
    }

    private void Leech(Entity<ADTCrusherRageOnDetonateComponent> ent, EntityUid user)
    {
        if (ent.Comp.LeechMultiplier <= 1f || !TryComp<LeechOnMarkerComponent>(ent, out var leech))
            return;

        _damageable.TryChangeDamage(user, leech.Leech * (ent.Comp.LeechMultiplier - 1f), true, false, origin: ent.Owner);
    }

    private void OnRageRemoved(Entity<ADTCrusherRageStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_net.IsClient || TerminatingOrDeleted(args.Target))
            return;

        _status.TrySetStatusEffectDuration(args.Target, ent.Comp.ExhaustionEffect, ent.Comp.ExhaustionDuration);
        _popup.PopupEntity(Loc.GetString("adt-crusher-rage-end-popup"), args.Target, args.Target, PopupType.Medium);
    }

    private void OnTrophyExamine(Entity<ADTCrusherRageOnDetonateComponent> ent, ref TrophyHolderExamineEvent args)
    {
        args.PushMarkup(Loc.GetString("adt-crusher-rage-examine",
            ("stacks", ent.Comp.MaxStacks),
            ("duration", ent.Comp.RageDuration.TotalSeconds.ToString("0.#")),
            ("max", ent.Comp.MaxRageDuration.TotalSeconds.ToString("0.#"))));
    }
}
