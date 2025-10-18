using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Server.StationEvents.Events;
using Content.Server.ADT.StationEvents.Components;
using Content.Server.Chat.Systems;

namespace Content.Server.ADT.StationEvents.Events;

public sealed class HeroEvent : StationEventSystem<HeroEventComponent>
{

    [Dependency] private readonly ChatSystem _chatSystem = default!;

    protected override void Added(EntityUid uid, HeroEventComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        _chatSystem.DispatchGlobalAnnouncement(
            message: Loc.GetString("hero-event-announcement"),
            sender: Loc.GetString("hero-event-sender"),
            colorOverride: Color.DarkOrange
        );
    }
}