using System.Numerics;
using Content.Shared.ADT.Cooking.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Cooking.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class ADTStoveComponent : Component
{
    public const string SlotContainerPrefix = "adt_stove_slot_";

    [DataField, AutoNetworkedField]
    public List<ADTStoveSlot> Slots = new();

    [DataField]
    public HashSet<ProtoId<ADTCookingRecipePrototype>> SecretRecipes = new();

    [DataField]
    public Vector2 CookwareScale = new(0.7f, 0.7f);

    [DataField]
    public string FlameRsi = "ADT/Structures/Machines/oven.rsi";

    [DataField]
    public string BurnerFlameState = "burner_";

    [DataField]
    public string OvenState = "oven_on";

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? OvenTimerEnd;

    [DataField, AutoNetworkedField]
    public TimeSpan? OvenTimerRemaining;

    [DataField, AutoNetworkedField]
    public bool Broken;

    [DataField]
    public float CookTimeMultiplier = 1f;

    [DataField]
    public List<int> OvenTimerPresets = new() { 5, 10, 15, 20, 30, 50, 60, 90 };

    [DataField]
    public int OvenTimerMax = 600;

    public static string SlotContainerId(int index)
    {
        return SlotContainerPrefix + index;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ADTStoveSlot
{
    [DataField("slotType", required: true)]
    public ADTStoveSlotType Type = ADTStoveSlotType.Burner;

    [DataField]
    public Vector2 Offset;

    [DataField]
    public ADTStoveHeat Heat = ADTStoveHeat.Off;

    [DataField]
    public LocId Name = "adt-stove-slot-burner";

    [DataField]
    public string? FlameState;

    [DataField]
    public Vector2? Scale;

    [DataField]
    public Color? Tint;

    [DataField]
    public bool Mirror;

    public ADTStoveSlot()
    {
    }

    public ADTStoveSlot(ADTStoveSlot other)
    {
        Type = other.Type;
        Offset = other.Offset;
        Heat = other.Heat;
        Name = other.Name;
        FlameState = other.FlameState;
        Scale = other.Scale;
        Tint = other.Tint;
        Mirror = other.Mirror;
    }
}
