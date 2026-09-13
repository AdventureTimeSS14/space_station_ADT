//

using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared.Heretic.Components.PathSpecific.Lock;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class LockTrappedDoorComponent : Component
{
    [DataField]
    public EntProtoId<ProjectileComponent> ProjectileProto = "TentacleProjectile";

    [DataField]
    public SoundSpecifier DamageSound = new SoundCollectionSpecifier("ShadowDemonPunch");

    [DataField]
    public SoundSpecifier DeathSound = new SoundPathSpecifier("/Audio/ADT/Heretic/shadowdeath.ogg");

    [DataField]
    public SoundSpecifier AggroSound = new SoundCollectionSpecifier("ShadowDemonLaugh");

    [DataField]
    public TimeSpan JitterTime = TimeSpan.FromSeconds(3);

    [DataField]
    public FixedPoint2 DeathThreshold = 30;

    [DataField, AutoNetworkedField]
    public FixedPoint2 SustainedDamage;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan NextGrapple;

    [DataField]
    public TimeSpan GrappleDelay = TimeSpan.FromMilliseconds(300);

    [DataField, AutoNetworkedField]
    public EntityUid? GrappleTarget;

    [DataField]
    public SpriteSpecifier JointSprite =
        new SpriteSpecifier.Rsi(new ResPath("ADT/Heretic/Effects/effects.rsi"), "tentacle");
}
