using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Cooking;

[Serializable, NetSerializable]
public enum ADTCookwareType : byte
{
    Pan,
    Pot,
    Tray,
    Plate,
}

[Serializable, NetSerializable]
public enum ADTStoveSlotType : byte
{
    Burner,
    Oven,
}

[Serializable, NetSerializable]
public enum ADTStoveHeat : byte
{
    Off,
    Low,
    Medium,
    High,
}

[Serializable, NetSerializable]
public enum ADTCookingStage : byte
{
    Idle,
    Cooking,
    Done,
    Overcooked,
    Burnt,
    OnFire,
}

[Serializable, NetSerializable]
public enum ADTCookwareEffect : byte
{
    None,
    Steam,
    Sizzle,
    Smoke,
    Fire,
}

[Serializable, NetSerializable]
public enum ADTCookwareVisuals : byte
{
    MediumColor,
    MediumFill,
    Effect,
    BatterColor,
    BatterVisible,
}

[Serializable, NetSerializable]
public enum ADTCookwareVisualLayers : byte
{
    Medium,
    Batter,
}
