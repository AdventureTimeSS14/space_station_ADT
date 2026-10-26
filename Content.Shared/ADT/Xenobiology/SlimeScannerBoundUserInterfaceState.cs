using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Xenobiology;

[Serializable, NetSerializable]
public sealed class SlimeScannerBoundUserInterfaceState : BoundUserInterfaceState
{
    public NetEntity TargetEntity;
    public string? BreedName;
    public string? ColorHex;
    public float? MutationChance;
    public List<string>? Mutations;
    public int? ExtractsProduced;
    public List<ExtractReagentInfo>? Reagents;

    public SlimeScannerBoundUserInterfaceState(
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