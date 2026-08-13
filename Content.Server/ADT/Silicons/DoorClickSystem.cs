using Content.Shared.ADT.Silicons;
using Content.Shared.Doors.Components;
using Content.Shared.Electrocution;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.ADT.Silicons;
public sealed class DoorClickSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<DoorClickEvent>(OnDoorClick);
    }

    private void OnDoorClick(DoorClickEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user ||
            (!HasComp<StationAiHeldComponent>(user) && !HasComp<BorgChassisComponent>(user)))
            return;

        var target = GetEntity(msg.Target);
        if (!TryComp<DoorComponent>(target, out var door) ||
            !TryComp<StationAiWhitelistComponent>(target, out var whitelist) ||
            !whitelist.Enabled ||
            !_interaction.InRangeUnobstructed(user, target))
        {
            return;
        }

        switch (msg.Action)
        {
            case DoorClickAction.EmergencyAccess:
                RaiseLocalEvent(target,
                    new StationAiEmergencyAccessEvent
                    {
                        User = user,
                        EmergencyAccess = !TryComp(target, out AirlockComponent? airlock) || !airlock.EmergencyAccess,
                    });
                break;
            case DoorClickAction.Bolt:
                RaiseLocalEvent(target,
                    new StationAiBoltEvent
                    {
                        User = user,
                        Bolted = !TryComp(target, out DoorBoltComponent? bolt) || !bolt.BoltsDown,
                    });
                break;
            case DoorClickAction.Electrify:
                RaiseLocalEvent(target,
                    new StationAiElectrifiedEvent
                    {
                        User = user,
                        Electrified = !TryComp(target, out ElectrifiedComponent? electrified) || !electrified.Enabled,
                    });
                break;
        }
    }
}
