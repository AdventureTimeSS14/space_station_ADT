// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TileFireComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId<TileFireComponent>? Id;

    [DataField, AutoNetworkedField]
    public bool ExtinguishInstantly = true;

    [DataField, AutoNetworkedField]
    public float PatExtinguishMultiplier = 1;

    [DataField, AutoNetworkedField]
    public float SprayExtinguishMultiplier = 1;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan SpawnedAt;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(55);

    [DataField, AutoNetworkedField]
    public TimeSpan BigFireDuration = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public bool BurnsInVacuum;

    [DataField, AutoNetworkedField]
    public TimeSpan VacuumDuration = TimeSpan.FromSeconds(1.5);

    [ViewVariables]
    public TileFireVisuals Visual = TileFireVisuals.Four;

    [ViewVariables]
    public TimeSpan? ScheduledAt;
}

[Serializable, NetSerializable]
public enum TileFireLayers
{
    Base,
}

[Serializable, NetSerializable]
public enum TileFireVisuals
{
    One,
    Two,
    Three,
    Four,
}
