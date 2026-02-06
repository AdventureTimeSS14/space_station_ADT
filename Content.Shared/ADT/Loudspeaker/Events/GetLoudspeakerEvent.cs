namespace Content.Shared.ADT.Loudspeaker.Events;

[ByRefEvent]
public record struct GetLoudspeakerEvent(
    List<EntityUid> Loudspeakers);
