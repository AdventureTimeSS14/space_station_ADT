using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Xenobiology;

[Serializable, NetSerializable]
public sealed class SlimeScannerScannedMessage : BoundUserInterfaceMessage
{
    public NetEntity TargetEntity;
    public string? BreedName;
    public string? ColorHex;
    public float? MutationChance;
    public List<string>? Mutations;
    public int? ExtractsProduced;
    public List<ExtractReagentInfo>? Reagents;

    public SlimeScannerScannedMessage(
        NetEntity targetEntity,
        string? breedName,
        string? colorHex,
        float? mutationChance,
        List<string>? mutations,
        int? extractsProduced,
        List<ExtractReagentInfo>? reagents)
    {
        TargetEntity = targetEntity;
        BreedName = breedName;
        ColorHex = colorHex;
        MutationChance = mutationChance;
        Mutations = mutations;
        ExtractsProduced = extractsProduced;
        Reagents = reagents;
    }
}

[Serializable, NetSerializable]
public sealed class ExtractReagentInfo
{
    public string ReagentId;
    public string ColorHex;

    public ExtractReagentInfo(string reagentId, string colorHex)
    {
        ReagentId = reagentId;
        ColorHex = colorHex;
    }
}