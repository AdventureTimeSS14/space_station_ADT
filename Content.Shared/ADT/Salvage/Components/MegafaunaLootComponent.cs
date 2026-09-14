using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Salvage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MegafaunaLootComponent : Component
{
    [DataField]
    public EntProtoId? CrateProto = "ADTCrateNecropolis";

    [DataField]
    public List<MegafaunaLootEntry> Loot = [];

    [DataField]
    public List<EntProtoId> RandomLoot = [];

    [DataField]
    public bool DropOnDeath = true;

    [DataField]
    public bool DeleteOnDrop;

    [DataField]
    public SoundSpecifier? DropSound;

    [DataField]
    public EntProtoId? DropEffect;

    [ViewVariables]
    public bool LootDropped;
}

[DataDefinition]
public sealed partial class MegafaunaLootEntry
{
    [DataField(required: true)]
    public EntProtoId Proto;

    [DataField]
    public int Min = 1;

    [DataField]
    public int Max = 1;
}
