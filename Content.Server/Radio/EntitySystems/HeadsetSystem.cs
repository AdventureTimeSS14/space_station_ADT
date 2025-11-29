using Content.Shared.Chat;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Server.ADT.Language;  // ADT Languages
using Content.Server.Popups; // ADT Radio Block
using Content.Shared.Cuffs.Components; // ADT Radio Block
using Content.Shared.StatusEffectNew.Components; // ADT Radio Block
using Content.Shared.Stunnable; // ADT Radio Block
using Robust.Shared.Localization; // ADT Radio Block

namespace Content.Server.Radio.EntitySystems;

public sealed class HeadsetSystem : SharedHeadsetSystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly LanguageSystem _language = default!;  // ADT Languages
    [Dependency] private readonly PopupSystem _popup = default!; // ADT Radio Block
    [Dependency] private readonly ILocalizationManager _loc = default!; // ADT Radio Block

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadsetComponent, RadioReceiveEvent>(OnHeadsetReceive);
        SubscribeLocalEvent<HeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);

        SubscribeLocalEvent<WearingHeadsetComponent, EntitySpokeEvent>(OnSpeak);
    }

    // ADT-ADD-Start: (P4A) Блокировка связи при оглушении и наручниках
    private bool BlockedByHandcuffsOrStun(EntityUid user)
    {
        if (TryComp<CuffableComponent>(user, out var cuffable))
        {
            if (cuffable.CuffedHandCount > 0)
                return true;
        }

        if (HasComp<StunnedComponent>(user))
            return true;

        return false;
    }

    private void ShowRadioBlockedPopup(EntityUid user)
    {
        if (!TryComp(user, out ActorComponent? actor))
            return;

        string msg;

        if (TryComp<CuffableComponent>(user, out var cuffable) && cuffable.CuffedHandCount > 0)
            msg = Loc.GetString("radio-blocked-handcuffed");
        else
            msg = Loc.GetString("radio-blocked-stunned");

        _popup.PopupEntity(msg, user, user);
    }

    private void OnSpeak(EntityUid uid, WearingHeadsetComponent component, EntitySpokeEvent args)
    {
        var user = uid;

        if (args.Channel != null)
        {
            if (BlockedByHandcuffsOrStun(user))
            {
                args.Channel = null;
                ShowRadioBlockedPopup(user);
                return;
            }
        }

        if (args.Channel != null
            && TryComp(component.Headset, out EncryptionKeyHolderComponent? keys)
            && keys.Channels.Contains(args.Channel.ID))
        {
            _radio.SendRadioMessage(uid, args.Message, args.Channel, component.Headset);
            args.Channel = null;
        }
    }
    // ADT-ADD-End

    private void OnKeysChanged(EntityUid uid, HeadsetComponent component, EncryptionChannelsChangedEvent args)
    {
        UpdateRadioChannels(uid, component, args.Component);
    }

    private void UpdateRadioChannels(EntityUid uid, HeadsetComponent headset, EncryptionKeyHolderComponent? keyHolder = null)
    {
        // make sure to not add ActiveRadioComponent when headset is being deleted
        if (!headset.Enabled || MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        if (!Resolve(uid, ref keyHolder))
            return;

        if (keyHolder.Channels.Count == 0)
            RemComp<ActiveRadioComponent>(uid);
        else
            EnsureComp<ActiveRadioComponent>(uid).Channels = new(keyHolder.Channels);
    }

    protected override void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);
        if (component.IsEquipped && component.Enabled)
        {
            EnsureComp<WearingHeadsetComponent>(args.Equipee).Headset = uid;
            UpdateRadioChannels(uid, component);
        }
    }

    protected override void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        base.OnGotUnequipped(uid, component, args);
        RemComp<ActiveRadioComponent>(uid);
        RemComp<WearingHeadsetComponent>(args.Equipee);
    }

    public void SetEnabled(EntityUid uid, bool value, HeadsetComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Enabled == value)
            return;

        component.Enabled = value;
        Dirty(uid, component);

        if (!value)
        {
            RemCompDeferred<ActiveRadioComponent>(uid);

            if (component.IsEquipped)
                RemCompDeferred<WearingHeadsetComponent>(Transform(uid).ParentUid);
        }
        else if (component.IsEquipped)
        {
            EnsureComp<WearingHeadsetComponent>(Transform(uid).ParentUid).Headset = uid;
            UpdateRadioChannels(uid, component);
        }
    }

    private void OnHeadsetReceive(EntityUid uid, HeadsetComponent component, ref RadioReceiveEvent args)
    {
        // TODO: change this when a code refactor is done
        // this is currently done this way because receiving radio messages on an entity otherwise requires that entity
        // to have an ActiveRadioComponent

        var parent = Transform(uid).ParentUid;

        if (parent.IsValid())
        {
            var relayEvent = new HeadsetRadioReceiveRelayEvent(args);
            RaiseLocalEvent(parent, ref relayEvent);
        }

        if (TryComp(parent, out ActorComponent? actor))
        {
            // ADT Languages start
            if (_language.CanUnderstand(Transform(uid).ParentUid, args.Language))
                _netMan.ServerSendMessage(args.ChatMsg, actor.PlayerSession.Channel);
            else
                _netMan.ServerSendMessage(args.UnknownLanguageChatMsg, actor.PlayerSession.Channel);
            // ADT Languages end
        }
    }
}
