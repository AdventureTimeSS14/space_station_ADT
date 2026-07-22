using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Bubblegum.Loot;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodFrenzyComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromMinutes(2);

    [DataField, AutoNetworkedField]
    public TimeSpan EndsAt;

    [DataField]
    public EntProtoId WeaponProto = "ADTChainsawDoomslayer";

    [DataField]
    public string? Reagent = "Adminordrazine";

    [DataField]
    public float ReagentAmount = 25f;

    [DataField]
    public EntProtoId? Objective = "ADTRipAndTearObjective";

    [DataField]
    public EntityUid? SpawnedWeapon;

    [DataField]
    public EntityUid? ObjectiveEntity;

    [DataField]
    public EntityUid? MusicStream;

    [DataField]
    public bool Started;

    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/e1m1.ogg");
}
