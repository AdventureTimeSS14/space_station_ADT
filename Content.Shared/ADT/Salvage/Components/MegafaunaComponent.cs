using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Salvage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class MegafaunaComponent : Component
{
    [DataField]
    public bool Hardmode = false;

    [DataField]
    public TimeSpan SpeechCooldown = TimeSpan.Zero;

    [DataField]
    public float SpeechChance = 1f;

    [DataField]
    public string SpeechFont = "Blackcraft";

    [DataField]
    public int SpeechFontSize = 16;

    [DataField, AutoPausedField]
    public TimeSpan NextSpeechTime;
}
