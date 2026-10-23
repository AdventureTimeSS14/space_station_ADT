using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Mining.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTMineralComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Hardness = 1;

    [DataField, AutoNetworkedField]
    public int MaxHardness = 1;

    [DataField]
    public TimeSpan MineTime = TimeSpan.FromSeconds(4);

    [DataField]
    public int OreAmount;

    [DataField]
    public int ExplosionBonus = 2;

    [DataField]
    public float HeavyIntensity = 4f;

    [DataField]
    public float DevastationIntensity = 8f;

    [ViewVariables, AutoNetworkedField]
    public bool Breaking;
}

[Serializable, NetSerializable]
public enum ADTMineralVisuals : byte
{
    Cracks,
}

[Serializable, NetSerializable]
public enum ADTMineralCracks : byte
{
    None,
    One,
    Two,
}
