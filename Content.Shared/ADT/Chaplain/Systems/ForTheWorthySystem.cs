using Content.Shared.ADT.Armor;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
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
    [Dependency] private HolyRelicSystem _relic = default!;
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
        SubscribeLocalEvent<ForTheWorthyComponent, BeingEquippedAttemptEvent>(OnBeingEquipped);
        SubscribeLocalEvent<ForTheWorthyComponent, ArmorProtectAttemptEvent>(OnArmorProtect);
    }

    public bool IsWorthy(Entity<ForTheWorthyComponent> ent, EntityUid user)
    {
        return IsFaithful(ent, user) && _relic.HasRelic(user);
    }

    private bool IsFaithful(Entity<ForTheWorthyComponent> ent, EntityUid user)
    {
        if (HasComp<ChaplainComponent>(user))
            return true;

        return ent.Comp.AllowFollowers && HasComp<ChaplainFollowerComponent>(user);
    }

    private void OnGettingUsed(Entity<ForTheWorthyComponent> ent, ref GettingUsedAttemptEvent args)
    {
        if (IsWorthy(ent, args.User))
            return;

        args.Cancel();
        Deny(ent, args.User);
    }

    private void OnAttemptMelee(Entity<ForTheWorthyComponent> ent, ref AttemptMeleeEvent args)
    {
        if (args.Cancelled || IsWorthy(ent, args.User))
            return;

        args.Cancelled = true;
        Deny(ent, args.User);
    }

    private void OnShotAttempted(Entity<ForTheWorthyComponent> ent, ref ShotAttemptedEvent args)
    {
        if (IsWorthy(ent, args.User))
            return;

        args.Cancel();
        Deny(ent, args.User);
    }

    private void OnBeingEquipped(Entity<ForTheWorthyComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled || !ent.Comp.RestrictEquip || IsWorthy(ent, args.EquipTarget))
            return;

        args.Cancel();
        args.Reason = IsFaithful(ent, args.EquipTarget) ? "chaplain-relic-required" : "chaplain-for-the-worthy-equip";
    }

    private void OnArmorProtect(Entity<ForTheWorthyComponent> ent, ref ArmorProtectAttemptEvent args)
    {
        if (!IsWorthy(ent, args.Wearer))
            args.Cancelled = true;
    }

    private void Deny(Entity<ForTheWorthyComponent> ent, EntityUid user)
    {
        if (_net.IsClient || _timing.CurTime < ent.Comp.NextPunish)
            return;

        ent.Comp.NextPunish = _timing.CurTime + ent.Comp.PunishDelay;

        if (IsFaithful(ent, user))
        {
            _popup.PopupEntity(Loc.GetString("chaplain-relic-required"), user, user, PopupType.MediumCaution);
            return;
        }

        Punish(ent, user);
    }

    private void Punish(Entity<ForTheWorthyComponent> ent, EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("chaplain-for-the-worthy"), user, user, PopupType.MediumCaution);
        _audio.PlayPvs(ent.Comp.ElectrocutionSound, user);
        _damageable.TryChangeDamage(user, new DamageSpecifier { DamageDict = { [ShockDamage] = ent.Comp.Damage } }, origin: ent);
        _stun.TryUpdateParalyzeDuration(user, ent.Comp.StunTime);
        _jittering.DoJitter(user, ent.Comp.StunTime, true, 80, 8);
        _stuttering.DoStutter(user, ent.Comp.StunTime + TimeSpan.FromSeconds(1), true);
    }
}
