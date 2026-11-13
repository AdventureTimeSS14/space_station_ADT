using Content.Shared.Thief;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Revolutinary.Stash;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTRevolutionKitComponent : Component
{
    [DataField]
    public List<ProtoId<ThiefBackpackSetPrototype>> PossibleSets = new();

    [DataField]
    public List<int> SelectedSets = new();

    [DataField]
    public int MaxSelectedSets = 2;

    [DataField]
    public SoundCollectionSpecifier ApproveSound = new("storageRustle");

    [DataField]
    public LocId ToolName = "adt-revolution-kit-window-title";

    [DataField]
    public LocId ToolDesc = "adt-revolution-kit-window-description";
}
