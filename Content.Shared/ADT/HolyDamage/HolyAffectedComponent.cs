using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.HolyDamage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HolyAffectedComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Piercing = true;

    [DataField, AutoNetworkedField]
    public bool BibleSmite;

    [DataField, AutoNetworkedField]
    public bool Suppressible;

    [DataField, AutoNetworkedField]
    public FixedPoint2? SuppressionFirstHit;

    [DataField, AutoNetworkedField]
    public FixedPoint2? SuppressionHit;

    [DataField, AutoNetworkedField]
    public FixedPoint2? SuppressionRecovery;

    [DataField, AutoNetworkedField]
    public float Power;

    [DataField, AutoNetworkedField]
    public float MinPower;

    [DataField, AutoNetworkedField]
    public float BaseVulnerability = 1f;

    [DataField, AutoNetworkedField]
    public float VulnerabilityPerPower;

    [DataField, AutoNetworkedField]
    public float MaxVulnerability = 1f;
}
