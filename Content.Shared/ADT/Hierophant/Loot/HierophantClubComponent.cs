using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Hierophant.Loot;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class HierophantClubComponent : Component
{
    [DataField]
    public TimeSpan CooldownTime = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan ChaserCooldown = TimeSpan.FromSeconds(8.1);

    [DataField]
    public TimeSpan MeleeCooldown = TimeSpan.FromSeconds(0.8);

    [DataField, AutoPausedField]
    public TimeSpan NextUseAt;

    [DataField, AutoPausedField]
    public TimeSpan NextChaserAt;

    [DataField]
    public float ChaserSpeed = 0.08f;

    [DataField]
    public int BlastRange = 13;

    [DataField]
    public float CardinalDamage = 30f;

    [DataField]
    public float MeleeDamage = 15f;

    [DataField]
    public float ChaserDamage = 30f;

    [DataField]
    public float Range = 5f;

    [DataField]
    public EntityUid? Beacon;

    [DataField]
    public bool FriendlyFireCheck;

    [DataField]
    public bool Teleporting;

    [ViewVariables]
    public readonly List<EntityUid> TeleportEffects = new();

    [DataField]
    public EntProtoId TelegraphEdgeProto = "ADTHierophantTelegraphEdge";

    [DataField]
    public TimeSpan BeaconDetachTime = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan TeleportChannelTime = TimeSpan.FromSeconds(3.7);

    [DataField]
    public float MinimumTeleportDistance = 2f;

    [DataField]
    public EntProtoId BeaconProto = "ADTHierophantBeacon";

    [DataField]
    public SoundSpecifier BeaconSound = new SoundPathSpecifier("/Audio/ADT/Hierophant/blind.ogg");

    [DataField]
    public SoundSpecifier TelegraphSound = new SoundPathSpecifier("/Audio/ADT/Hierophant/bin_close.ogg");

    [DataField]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/ADT/Hierophant/wand_teleport.ogg");

    [DataField]
    public SoundSpecifier DepartSound = new SoundPathSpecifier("/Audio/ADT/Hierophant/airlock_open.ogg");
}
