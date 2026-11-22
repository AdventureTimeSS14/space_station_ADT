using Content.Shared.ADT.Language;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Xenobiology.Potions;

/// <summary>
/// A potion that gives the target an intrinsic radio so it can always use the radio.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeBluespaceRadioPotionComponent : Component
{
    /// <summary>
    /// The set of channels the recipient of the potion will subscribe to.
    /// </summary>
    [DataField("channels", required: true)]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = default!;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeBluespaceRadioComponent : Component
{
}

public sealed partial class SlimeBluespaceRadioPotionSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeBluespaceRadioPotionComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SlimeBluespaceRadioComponent, RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
    }

    private void OnAfterInteract(Entity<SlimeBluespaceRadioPotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        if (!HasComp<LanguageSpeakerComponent>(target))
            return;

        args.Handled = true;

        var activeRadio = EnsureComp<ActiveRadioComponent>(target);
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(target);
        foreach (var channel in ent.Comp.Channels)
        {
            activeRadio.Channels.Add(channel);
            transmitter.Channels.Add(channel);
        }
        Dirty(target, transmitter);
        Dirty(target, activeRadio);
        EnsureComp<IntrinsicRadioReceiverComponent>(target);
        EnsureComp<SlimeBluespaceRadioComponent>(target);

        _popup.PopupPredicted(Loc.GetString("xeno-potion-radio-applied", ("name", Name(target))), target, target);
        PredictedQueueDel(args.Used);
    }

    private void OnRadioReceiveAttempt(Entity<SlimeBluespaceRadioComponent> ent, ref RadioReceiveAttemptEvent args)
    {
        if (TryComp<WearingHeadsetComponent>(ent, out var wearingHeadset)
            && TryComp<EncryptionKeyHolderComponent>(wearingHeadset.Headset, out var headsetKeys)
            && headsetKeys.Channels.Contains(args.Channel.ID))
            args.Cancelled = true;
    }
}
