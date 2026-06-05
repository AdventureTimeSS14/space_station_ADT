using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.ADT.Cyberpsychosis;

[RegisterComponent]
public sealed partial class CyberpsychosisComponent : Component
{
    [DataField]
    public int MaxLoad = 100;

    [DataField]
    public int CurrentLoad = 0;

    public CyberpsychosisState CurrentState = CyberpsychosisState.None;

    public bool InEpisode = false;
    public TimeSpan EpisodeEnd = TimeSpan.Zero;

    // Стресс
    [ViewVariables(VVAccess.ReadWrite)]
    public float StressLevel = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float StressIncrement = 1f;

    public TimeSpan NextStressTick = TimeSpan.Zero;

    [DataField]
    public float StressThreshold = 100f;

    // Диапазоны инкремента по стадиям
    [DataField] public float MildIncrMin = 0.8f;
    [DataField] public float MildIncrMax = 1.2f;
    [DataField] public float ModerateIncrMin = 3.5f;
    [DataField] public float ModerateIncrMax = 5.5f;
    [DataField] public float SevereIncrMin = 7.0f;
    [DataField] public float SevereIncrMax = 10.0f;

    // Длительность эпизода
    [DataField]
    public float MinDuration = 5f;

    [DataField]
    public float DurationPerOverflowUnit = 1f;

    [DataField]
    public string MildHallucinationPack = "CyberpsychosisHallucinationsMild";
}
