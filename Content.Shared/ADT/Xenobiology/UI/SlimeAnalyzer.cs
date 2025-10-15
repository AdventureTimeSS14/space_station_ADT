using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Xenobiology.UI;

[Serializable, NetSerializable]
public enum SlimeAnalyzerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SlimeAnalyzerScannedUserMessage : BoundUserInterfaceMessage
{
    public NetEntity TargetEntity;
    public float Hunger;
    public float MaxHunger;
    public string GrowthStage;
    public string SlimeType;
    public float MutationChance;
    public float SpecialMutationChance;
    public List<string>? PotentialMutations;

    public SlimeAnalyzerScannedUserMessage(
        NetEntity targetEntity,
        float hunger,
        float maxHunger,
        string growthStage,
        string slimeType,
        float mutationChance,
        float specialMutationChance,
        List<string> potentialMutations)
    {
        TargetEntity = targetEntity;
        Hunger = hunger;
        MaxHunger = maxHunger;
        GrowthStage = growthStage;
        SlimeType = slimeType;
        MutationChance = mutationChance;
        SpecialMutationChance = specialMutationChance;
        PotentialMutations = potentialMutations;
    }
}
