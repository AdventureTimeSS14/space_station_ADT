using Content.Server.ADT.Radiation;
using Content.Shared.Popups;
using Content.Shared.Radiation.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.ADT.Radiation;

public sealed class RadiationNoticingSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, OnIrradiatedEvent>(OnIrradiated);
    }

    private void OnIrradiated(EntityUid uid, ActorComponent actorComponent, OnIrradiatedEvent args)
    {
        if (!HasComp<RadiationPopupComponent>(uid))
            return;

        // Roll chance for popup messages
        // This is per radiation update tick,
        // is tuned so that when being irradiated with 1 rad/sec, see 1 message every 5-ish seconds on average
        if (_random.NextFloat() <= args.RadsPerSecond / 20)
        {
            SendRadiationPopup(uid);
        }

        //TODO: Expand system with other effects: visual spots, vomiting blood?, blurry vision?
    }

    private void SendRadiationPopup(EntityUid uid)
    {
        // Todo: detect possessing specific types of organs/blood/etc and conditionally add related messages to the list

        // pick a random message
        var msgId = _random.Pick(RadiationPopupComponent.msgArr);
        var msg = Loc.GetString(msgId);

        // show it as a popup
        _popupSystem.PopupEntity(msg, uid, uid, PopupType.LargeCaution);
    }
}