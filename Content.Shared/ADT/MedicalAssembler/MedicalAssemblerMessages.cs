using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MedicalAssembler;

[Serializable, NetSerializable]
public enum MedicalAssemblerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MedicalAssemblerCraftMessage : BoundUserInterfaceMessage
{
    public string RecipeId;

    public MedicalAssemblerCraftMessage(string recipeId)
    {
        RecipeId = recipeId;
    }
}

[Serializable, NetSerializable]
public sealed class MedicalAssemblerEjectMaterialsMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class MedicalAssemblerCreateCustomMessage : BoundUserInterfaceMessage
{
    public string ItemType;
    public string StyleId;

    public List<ReagentQuantity> Reagents;

    public MedicalAssemblerCreateCustomMessage(string itemType, string styleId, List<ReagentQuantity> reagents)
    {
        ItemType = itemType;
        StyleId = styleId;
        Reagents = reagents;
    }
}

[Serializable, NetSerializable]
public sealed class MedicalAssemblerRecipeEntry
{
    public string RecipeId;
    public bool Unlocked;

    public MedicalAssemblerRecipeEntry(string recipeId, bool unlocked)
    {
        RecipeId = recipeId;
        Unlocked = unlocked;
    }
}

[Serializable, NetSerializable]
public sealed class MedicalAssemblerMaterialEntry
{
    public string PrototypeId;
    public int Count;

    public MedicalAssemblerMaterialEntry(string prototypeId, int count)
    {
        PrototypeId = prototypeId;
        Count = count;
    }
}

[Serializable, NetSerializable]
public sealed class MedicalAssemblerUpdateState : BoundUserInterfaceState
{
    public MedicalAssemblerRecipeEntry[] Recipes;
    public MedicalAssemblerMaterialEntry[] Materials;
    public List<ReagentQuantity>? BeakerReagents;
    public float BeakerVolume;
    public float BeakerCapacity;
    public bool HasBeaker;
    public float ReagentMultiplier = 1f;
    public bool CustomTabUnlocked;
    public float MaxCustomVolume = 5f;

    public MedicalAssemblerUpdateState(
        MedicalAssemblerRecipeEntry[] recipes,
        MedicalAssemblerMaterialEntry[] materials,
        List<ReagentQuantity>? beakerReagents,
        float beakerVolume,
        float beakerCapacity,
        bool hasBeaker,
        float reagentMultiplier = 1f,
        bool customTabUnlocked = false,
        float maxCustomVolume = 5f)
    {
        Recipes = recipes;
        Materials = materials;
        BeakerReagents = beakerReagents;
        BeakerVolume = beakerVolume;
        BeakerCapacity = beakerCapacity;
        HasBeaker = hasBeaker;
        ReagentMultiplier = reagentMultiplier;
        CustomTabUnlocked = customTabUnlocked;
        MaxCustomVolume = maxCustomVolume;
    }
}
