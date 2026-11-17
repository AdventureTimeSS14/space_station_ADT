using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Chaplain.Systems;

public sealed class ForTheWorthySystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedJitteringSystem _jittering = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StutteringSystem _stuttering = default!;

    private static readonly ProtoId<DamageTypePrototype> ShockDamage = "Heat";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForTheWorthyComponent, GettingUsedAttemptEvent>(OnGettingUsed);
        SubscribeLocalEvent<ForTheWorthyComponent, AttemptMeleeEvent>(OnAttemptMelee);
        SubscribeLocalEvent<ForTheWorthyComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

    private void OnGettingUsed(Entity<ForTheWorthyComponent> ent, ref GettingUsedAttemptEvent args)
    {
        if (IsWorthy(args.User))
            return;

        args.Cancel();
        Punish(ent, args.User);
    }

    private void OnAttemptMelee(Entity<ForTheWorthyComponent> ent, ref AttemptMeleeEvent args)
    {
        if (args.Cancelled || IsWorthy(args.User))
            return;

        args.Cancelled = true;
        Punish(ent, args.User);
    }

    private void OnShotAttempted(Entity<ForTheWorthyComponent> ent, ref ShotAttemptedEvent args)
    {
        if (IsWorthy(args.User))
            return;

        args.Cancel();
        Punish(ent, args.User);
    }

    private bool IsWorthy(EntityUid user)
    {
        return HasComp<ChaplainComponent>(user);
    }

    private void Punish(Entity<ForTheWorthyComponent> ent, EntityUid user)
    {
        if (_net.IsClient || _timing.CurTime < ent.Comp.NextPunish)
            return;

        ent.Comp.NextPunish = _timing.CurTime + ent.Comp.PunishDelay;

        _popup.PopupEntity(Loc.GetString("chaplain-for-the-worthy"), user, user, PopupType.MediumCaution);
        _audio.PlayPvs(ent.Comp.ElectrocutionSound, user);
        _damageable.TryChangeDamage(user, new DamageSpecifier { DamageDict = { [ShockDamage] = ent.Comp.Damage } }, origin: ent);
        _stun.TryUpdateParalyzeDuration(user, ent.Comp.StunTime);
        _jittering.DoJitter(user, ent.Comp.StunTime, true, 80, 8);
        _stuttering.DoStutter(user, ent.Comp.StunTime + TimeSpan.FromSeconds(1), true);
    }
}
