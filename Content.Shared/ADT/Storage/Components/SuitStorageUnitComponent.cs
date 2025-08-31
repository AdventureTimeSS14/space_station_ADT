using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.SuitStorage;

[RegisterComponent, NetworkedComponent]
public sealed partial class SuitStorageUnitComponent : Component
{
    // Какие предметы может выдавать
    [DataField] public string? SuitPrototype;
    [DataField] public string? BootsPrototype;
    [DataField] public string? BreathMaskPrototype;
    [DataField] public string? StoragePrototype;

    // Флаг: выдан ли уже предмет
    [ViewVariables] public bool SuitTaken;
    [ViewVariables] public bool BootsTaken;
    [ViewVariables] public bool MaskTaken;
    [ViewVariables] public bool StorageTaken;
}