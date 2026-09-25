using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.ADT.Mobs;

public sealed class TryCatchBreathSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TryCatchBreathAlertEvent>(OnAlertClicked);
        SubscribeLocalEvent<TryCatchBreathComponent, TryCatchBreathDoAfterEvent>(OnDoAfter);
    }

    private void OnAlertClicked(TryCatchBreathAlertEvent ev)
    {
        if (!_net.IsServer)
            return;

        var uid = ev.User;

        if (!TryComp<TryCatchBreathComponent>(uid, out var comp))
            return;

        if (CompOrNull<MobStateComponent>(uid)?.CurrentState != MobState.SoftCritical)
            return;

        var args = new DoAfterArgs(EntityManager, uid, comp.DoAfterTime, new TryCatchBreathDoAfterEvent(), uid)
        {
            Broadcast = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            RequireCanInteract = false,
            CancelDuplicate = true,
            BlockDuplicate = true,
        };

        _doAfter.TryStartDoAfter(args);

        _popup.PopupEntity(Loc.GetString("catch-breath-try"), uid);
        _audio.PlayEntity(new SoundPathSpecifier(comp.AudioPath + "catch-breath-try.ogg"), uid, uid);

        _adminLogger.Add(LogType.CatchBreath, LogImpact.Low, $"{ToPrettyString(uid):user} started trying to catch their breath");
    }

    private void OnDoAfter(Entity<TryCatchBreathComponent> ent, ref TryCatchBreathDoAfterEvent ev)
    {
        if (!_net.IsServer || ev.Cancelled)
            return;

        var uid = ev.User;

        if (CompOrNull<MobStateComponent>(uid)?.CurrentState != MobState.SoftCritical)
            return;

        var audioPath = ent.Comp.AudioPath;
        var roll = _random.NextFloat();
        var damage = new DamageSpecifier();
        string popup;
        string sound;

        if (roll < 0.03f)
        {
            damage.DamageDict.Add("Blunt", -2);
            damage.DamageDict.Add("Slash", -2);
            damage.DamageDict.Add("Piercing", -2);
            damage.DamageDict.Add("Asphyxiation", -10);
            popup = "catch-breath-blunt-success";
            sound = "catch-breath-bluntsuccess.ogg";
        }
        else if (roll < 0.63f)
        {
            damage.DamageDict.Add("Asphyxiation", -7);
            popup = "catch-breath-success";
            sound = "catch-breath-success.ogg";
        }
        else if (roll < 0.78f)
        {
            damage.DamageDict.Add("Asphyxiation", 5);
            popup = "catch-breath-failure";
            sound = "catch-breath-failure.ogg";
        }
        else
        {
            popup = "catch-breath-nothing";
            sound = "catch-breath-nothing.ogg";
        }

        _adminLogger.Add(LogType.CatchBreath, LogImpact.Low, $"{ToPrettyString(uid):user} rolled {roll} trying to catch their breath: {popup}");

        _popup.PopupEntity(Loc.GetString(popup), uid);
        _audio.PlayEntity(new SoundPathSpecifier(audioPath + sound), uid, uid);

        if (damage.DamageDict.Count > 0)
            _damage.TryChangeDamage(uid, damage);

        ev.Repeat = false;
    }
}
