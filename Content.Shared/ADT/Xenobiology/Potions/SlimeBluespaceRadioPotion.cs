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

public sealed partial class SlimeBluespaceRadioPotionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeBluespaceRadioPotionComponent, AfterInteractEvent>(OnAfterInteract);
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

        _popup.PopupPredicted(Loc.GetString("xeno-potion-radio-applied", ("name", Name(target))), target, target);
        PredictedQueueDel(args.Used);
    }
}
