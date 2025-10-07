using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Vore.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VoredComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Devourer;

    [DataField]
    public float AccumulatedTime = 0f;

    [DataField]
    public float DamageInterval = 1f;

    [DataField]
    public float AcidDamage = 5f;
}

