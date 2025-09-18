using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Server.StationEvents.Events;
using Content.Server.ADT.StationEvents.Components;
using Content.Server.Chat.Systems;

namespace Content.Server.ADT.StationEvents.Events;

public sealed class IanHeroEvent : StationEventSystem<IanHeroEventComponent>
{

    [Dependency] private readonly ChatSystem _chatSystem = default!;

    protected override void Added(EntityUid uid, IanHeroEventComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        _chatSystem.DispatchGlobalAnnouncement(
            message: Loc.GetString("ian-hero-event-announcement"),
            sender: Loc.GetString("ian-hero-event-sender"),
            colorOverride: Color.DarkOrange
        );
    }
}
