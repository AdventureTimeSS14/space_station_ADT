using Content.Shared.Chat;
using Content.Shared.Speech.Components; // ADT-Tweak

namespace Content.Server.Speech.EntitySystems;

public sealed partial class VoiceOverrideSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeTTS();    // ADT TTS
        InitializeBarks();  // ADT Barks
    }
}
