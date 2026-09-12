using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Telephone;
using Content.Shared.ADT.Telephone;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Content.Shared.Telephone;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Telephone;

/// <summary>
/// Human interface for the handheld telephones: call list, answer, hang up and do-not-disturb.
/// </summary>
public sealed class ADTPhoneSystem : EntitySystem
{
    [Dependency] private readonly TelephoneSystem _telephone = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ADTPhoneComponent, UseInHandEvent>(OnUseInHand, before: [typeof(ActivatableUISystem)]);
        SubscribeLocalEvent<ADTPhoneComponent, BeforeActivatableUIOpenEvent>(OnBeforeOpen);
        SubscribeLocalEvent<ADTPhoneComponent, TelephoneStateChangeEvent>(OnStateChanged);

        Subs.BuiEvents<ADTPhoneComponent>(ADTPhoneUiKey.Key, subs =>
        {
            subs.Event<ADTPhoneCallMsg>(OnCallMsg);
            subs.Event<ADTPhoneDoNotDisturbMsg>(OnDoNotDisturbMsg);
            subs.Event<ADTPhoneAnswerMsg>(OnAnswerMsg);
            subs.Event<ADTPhoneHangUpMsg>(OnHangUpMsg);
        });
    }

    private void OnBeforeOpen(Entity<ADTPhoneComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        SendUIState(ent.Owner);
    }

    private void OnUseInHand(Entity<ADTPhoneComponent> ent, ref UseInHandEvent args)
    {
        if (TryAnswerOrHangUp(ent, args.User))
            args.Handled = true;
    }

    private bool TryAnswerOrHangUp(Entity<ADTPhoneComponent> ent, EntityUid user)
    {
        if (!TryComp<TelephoneComponent>(ent.Owner, out var phone))
            return false;

        if (phone.CurrentState == TelephoneState.Ringing)
        {
            _telephone.AnswerTelephone((ent.Owner, phone), user);
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} answered {ToPrettyString(ent.Owner)}");
            return true;
        }

        if (phone.CurrentState != TelephoneState.Idle)
        {
            _telephone.EndTelephoneCalls((ent.Owner, phone));
            _popup.PopupEntity(Loc.GetString("adt-phone-hung-up"), ent.Owner, user, PopupType.Medium);
            return true;
        }

        return false;
    }

    private void OnCallMsg(Entity<ADTPhoneComponent> ent, ref ADTPhoneCallMsg args)
    {
        if (!_hands.IsHolding(args.Actor, ent.Owner))
            return;

        if (!TryComp<TelephoneComponent>(ent.Owner, out var phone))
            return;

        var time = _timing.CurTime;
        if (time < ent.Comp.LastCall + ent.Comp.CallCooldown)
            return;

        if (_telephone.IsTelephoneEngaged((ent.Owner, phone)))
            return;

        if (GetEntity(args.Id) is not { Valid: true } target ||
            target == ent.Owner ||
            !TryComp<ADTPhoneComponent>(target, out var targetComp) ||
            !TryComp<TelephoneComponent>(target, out var targetPhone))
        {
            return;
        }

        ent.Comp.LastCall = time;

        if (targetComp.DoNotDisturb)
        {
            _audio.PlayPvs(ent.Comp.BusySound, ent.Owner);
            _popup.PopupEntity(Loc.GetString("adt-phone-call-do-not-disturb"), ent.Owner, args.Actor, PopupType.MediumCaution);
            return;
        }

        if (_telephone.IsTelephoneEngaged((target, targetPhone)))
        {
            _audio.PlayPvs(ent.Comp.BusySound, ent.Owner);
            _popup.PopupEntity(Loc.GetString("adt-phone-call-busy"), ent.Owner, args.Actor, PopupType.MediumCaution);
            return;
        }

        _telephone.CallTelephone((ent.Owner, phone), (target, targetPhone), args.Actor);

        // The call can still fail if the receiver changed state between the checks above.
        if (phone.CurrentState == TelephoneState.Idle)
        {
            _audio.PlayPvs(ent.Comp.BusySound, ent.Owner);
            _popup.PopupEntity(Loc.GetString("adt-phone-call-busy"), ent.Owner, args.Actor, PopupType.MediumCaution);
            return;
        }

        _popup.PopupEntity(Loc.GetString("adt-phone-calling"), ent.Owner, args.Actor);

        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Actor)} called {ToPrettyString(target)} from {ToPrettyString(ent.Owner)}");

        SendUIState(ent.Owner);
    }

    private void OnDoNotDisturbMsg(Entity<ADTPhoneComponent> ent, ref ADTPhoneDoNotDisturbMsg args)
    {
        if (!_hands.IsHolding(args.Actor, ent.Owner))
            return;

        ent.Comp.DoNotDisturb = args.DoNotDisturb;
        SendUIState(ent.Owner);
    }

    private void OnAnswerMsg(Entity<ADTPhoneComponent> ent, ref ADTPhoneAnswerMsg args)
    {
        if (!_hands.IsHolding(args.Actor, ent.Owner))
            return;

        if (!TryComp<TelephoneComponent>(ent.Owner, out var phone))
            return;

        _telephone.AnswerTelephone((ent.Owner, phone), args.Actor);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Actor)} answered {ToPrettyString(ent.Owner)}");
    }

    private void OnHangUpMsg(Entity<ADTPhoneComponent> ent, ref ADTPhoneHangUpMsg args)
    {
        if (!_hands.IsHolding(args.Actor, ent.Owner))
            return;

        if (!TryComp<TelephoneComponent>(ent.Owner, out var phone))
            return;

        _telephone.EndTelephoneCalls((ent.Owner, phone));
        _popup.PopupEntity(Loc.GetString("adt-phone-hung-up"), ent.Owner, args.Actor, PopupType.Medium);
    }

    private void OnStateChanged(Entity<ADTPhoneComponent> ent, ref TelephoneStateChangeEvent args)
    {
        switch (args.NewState)
        {
            case TelephoneState.Calling:
                _audio.PlayPvs(ent.Comp.RingOutgoingSound, ent.Owner);
                break;

            case TelephoneState.Ringing:
                if (GetHolder(ent.Owner) is { } holder)
                    _popup.PopupEntity(Loc.GetString("adt-phone-ringing"), ent.Owner, holder, PopupType.Medium);
                else
                    _popup.PopupEntity(Loc.GetString("adt-phone-ringing"), ent.Owner, PopupType.Medium);
                break;

            case TelephoneState.InCall:
                _audio.PlayPvs(ent.Comp.PickupSound, ent.Owner);
                break;

            case TelephoneState.EndingCall:
                _audio.PlayPvs(ent.Comp.HangUpSound, ent.Owner);
                break;
        }

        SendUIState(ent.Owner);
    }

    private EntityUid? GetHolder(EntityUid phone)
    {
        if (_container.TryGetContainingContainer((phone, null, null), out var container) &&
            HasComp<InventoryComponent>(container.Owner))
        {
            return container.Owner;
        }

        return null;
    }

    private string GetPhoneName(EntityUid phone)
    {
        if (GetHolder(phone) is { } holder)
        {
            var name = Identity.Name(holder, EntityManager);
            if (_idCard.TryFindIdCard(holder, out var idCard))
                return $"{name} ({idCard.Comp.LocalizedJobTitle})";

            return name;
        }

        return Name(phone);
    }

    private void SendUIState(EntityUid phone)
    {
        if (!TryComp<TelephoneComponent>(phone, out var phoneComp))
            return;

        var phones = new List<ADTPhoneInfo>();
        var query = EntityQueryEnumerator<ADTPhoneComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid == phone)
                continue;

            phones.Add(new ADTPhoneInfo(GetNetEntity(uid), GetPhoneName(uid)));
        }

        var state = new ADTPhoneBuiState(
            phones,
            Comp<ADTPhoneComponent>(phone).DoNotDisturb,
            _telephone.IsTelephoneEngaged((phone, phoneComp)),
            phoneComp.CurrentState == TelephoneState.Ringing);

        _ui.SetUiState(phone, ADTPhoneUiKey.Key, state);
    }
}
