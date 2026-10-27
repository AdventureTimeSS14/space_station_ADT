using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Body.OrganHealth;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OrganHealthSystem))]
public sealed partial class OrganHealthComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Damage;

    [DataField]
    public float MaxDamage = 100f;

    [DataField]
    public float LowThreshold = 0.1f;

    [DataField]
    public float HighThreshold = 0.45f;

    [DataField]
    public float HealRate = 0.05f;

    [DataField]
    public OrganKind Kind = OrganKind.Organic;

    [ViewVariables, AutoNetworkedField]
    public OrganStage Stage = OrganStage.Healthy;
}

[Serializable, NetSerializable]
public enum OrganKind : byte
{
    Organic,
    Robotic,
}

[Serializable, NetSerializable]
public enum OrganStage : byte
{
    Healthy,
    Bruised,
    Damaged,
    Failing,
}

[ByRefEvent]
public readonly record struct OrganStageChangedEvent(EntityUid? Body, OrganStage OldStage, OrganStage NewStage);
