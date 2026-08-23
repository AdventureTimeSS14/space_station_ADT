using Content.Shared.ADT.OreFurnace.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.OreFurnace.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTOreFurnaceComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<OreSmeltPackPrototype>> Packs = new();

    [DataField, AutoNetworkedField]
    public int MaxSmeltAmount = 30;

    [DataField, AutoNetworkedField]
    public int DefaultAmount = 30;

    [DataField, AutoNetworkedField]
    public float MaterialUseMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public float OutputMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public float PointsMultiplier = 1f;

    [DataField]
    public SoundSpecifier? SmeltSound;
}
