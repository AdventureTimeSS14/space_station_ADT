using Content.Server.Administration.Logs;
using Content.Shared.ADT.Mobs;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Random;

namespace Content.Server.ADT.Mobs;

public sealed class TryCatchBreathSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TryCatchBreathComponent, TryCatchBreathAlertEvent>(OnAlertClicked);
        SubscribeLocalEvent<TryCatchBreathComponent, TryCatchBreathDoAfterEvent>(OnDoAfter);
    }

    private void OnAlertClicked(Entity<TryCatchBreathComponent> ent, ref TryCatchBreathAlertEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!_mobState.IsSoftCritical(ent.Owner))
            return;

        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.DoAfterTime, new TryCatchBreathDoAfterEvent(), ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            RequireCanInteract = false,
            BlockDuplicate = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(Loc.GetString(ent.Comp.TryPopup), ent, ent);
        _audio.PlayEntity(ent.Comp.TrySound, ent, ent);
        _adminLogger.Add(LogType.CatchBreath, LogImpact.Low, $"{ToPrettyString(ent):user} started trying to catch their breath");
    }

    private void OnDoAfter(Entity<TryCatchBreathComponent> ent, ref TryCatchBreathDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (!_mobState.IsSoftCritical(ent.Owner) || PickOutcome(ent.Comp) is not { } outcome)
            return;

        _popup.PopupEntity(Loc.GetString(outcome.Popup), ent, ent);
        _audio.PlayEntity(outcome.Sound, ent, ent);

        if (outcome.Damage != null)
            _damage.TryChangeDamage(ent.Owner, outcome.Damage, true);

        _adminLogger.Add(LogType.CatchBreath, LogImpact.Low, $"{ToPrettyString(ent):user} tried to catch their breath: {outcome.Popup}");
    }

    private CatchBreathOutcome? PickOutcome(TryCatchBreathComponent comp)
    {
        var total = 0f;
        foreach (var outcome in comp.Outcomes)
        {
            total += outcome.Weight;
        }

        var roll = _random.NextFloat(total);
        foreach (var outcome in comp.Outcomes)
        {
            roll -= outcome.Weight;
            if (roll < 0f)
                return outcome;
        }

        return null;
    }
}
