using Content.Shared.ADT.Salvage.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Salvage.Components;

/// <summary>
/// Предназначение понятно по названию
/// Чисто серверный компонент. Нам не нужен предикт спавна npc
/// </summary>
[RegisterComponent]
public sealed partial class TendrilComponent : Component
{
    [DataField]
    public int MaxSpawns = 3;

    [DataField]
    public float SpawnDelay = 10f;

    [DataField]
    public float ChasmDelay = 5f;

    [DataField]
    public int ChasmRadius = 2;

    [DataField(required: true)]
    public List<EntProtoId> Spawns;

    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntityUid> Mobs = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastSpawn = TimeSpan.Zero;
}
