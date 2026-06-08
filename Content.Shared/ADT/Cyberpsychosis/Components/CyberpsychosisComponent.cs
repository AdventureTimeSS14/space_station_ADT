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
    public float MildIncrMin = 0.8f;
    public float MildIncrMax = 1.2f;
    public float ModerateIncrMin = 3.5f;
    public float ModerateIncrMax = 5.5f;
    public float SevereIncrMin = 7.0f;
    public float SevereIncrMax = 10.0f;

    // Длительность эпизода
    [DataField]
    public float MinDuration = 5f;

    [DataField]
    public float DurationPerOverflowUnit = 1f;

    [DataField]
    public string MildHallucinationPack = "CyberpsychosisHallucinationsMild";

    // Шансы стана
    public float MildParalyzeChance = 0.01f;
    public float ModerateParalyzeChance = 0.20f;
    public float SevereParalyzeChance = 1f;

    public TimeSpan ParalyzeDuration = TimeSpan.FromSeconds(10);
}
