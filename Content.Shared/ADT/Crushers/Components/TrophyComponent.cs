using Content.Shared.ADT.Crushers.Effects;
using Content.Shared.ADT.Crushers.Systems;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(TrophyHolderSystem), typeof(TrophySystem))]
public sealed partial class TrophyComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<TrophyEffect> Effects = new();

    [DataField, AutoNetworkedField]
    public LocId ExamineText;

    [DataField]
    public ProtoId<TagPrototype> TrophyTag = default!;
}
