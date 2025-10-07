using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Vore.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedVoreSystem))]
public sealed partial class VoreComponent : Component
{
    [DataField]
    public EntProtoId VoreAction = "ActionVore";

    [DataField, AutoNetworkedField]
    public EntityUid? VoreActionEntity;

    [DataField]
    public EntProtoId ReleaseAction = "ActionVoreRelease";

    [DataField, AutoNetworkedField]
    public EntityUid? ReleaseActionEntity;

    [DataField, AutoNetworkedField]
    public int MaxCapacity = 2;

    [DataField]
    public float VoreTime = 3f;

    [DataField]
    public SoundSpecifier VoreSound = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    [DataField]
    public SoundSpecifier ReleaseSound = new SoundPathSpecifier("/Audio/Effects/gib2.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public Container Stomach = default!;

    [DataField, AutoNetworkedField]
    public List<EntityUid> VoredEntities = new();
}

