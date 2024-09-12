using Content.Shared.Preferences;

namespace Content.Shared.Humanoid;  // ADT File

[ByRefEvent]
public record struct HumanoidProfileLoadedEvent(HumanoidCharacterProfile Profile);
