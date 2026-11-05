using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTCrusherSpearThrowComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Range = 6f;

    [DataField, AutoNetworkedField]
    public float ThrowSpeed = 18f;

    [DataField, AutoNetworkedField]
    public float ReturnSpeed = 22f;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public bool RequireWielded = true;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public TimeSpan MarkDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public string DelayId = "adtSpearThrow";

    [DataField]
    public SoundSpecifier? ThrowSound;

    [DataField]
    public SoundSpecifier? HitSound;

    [DataField]
    public SoundSpecifier? CatchSound;

    [DataField]
    public EntProtoId? HitEffect;

    [DataField]
    public EntProtoId? MarkProjectile;

    [DataField]
    public float MarkProjectileSpeed = 30f;

    [DataField]
    public float CatchDistance = 0.7f;

    [DataField]
    public TimeSpan MaxReturnTime = TimeSpan.FromSeconds(3);

    [DataField]
    public float TrailFrequency = 0.03f;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Thrower;

    [ViewVariables, AutoNetworkedField]
    public bool Returning;

    [ViewVariables]
    public EntityCoordinates? ThrowOrigin;

    [ViewVariables]
    public TimeSpan NextRequest;
}
