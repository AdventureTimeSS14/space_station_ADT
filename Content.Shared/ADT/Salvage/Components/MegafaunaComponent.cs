using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Salvage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class MegafaunaComponent : Component
{
    [DataField]
    public TimeSpan AggroMemory = TimeSpan.FromSeconds(45);

    [DataField]
    public float AggroRadiusPadding = 5f;

    [DataField]
    public float MaxAggroRadius = 100f;

    [ViewVariables]
    public EntityUid? Aggressor;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan AggroEndTime;

    [ViewVariables]
    public float? BaseAggroRadius;

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
