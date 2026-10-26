using System.Linq;
using Content.Shared.ADT.EntityEffects;
using Content.Shared.EntityEffects;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects.Effects;

public sealed partial class SpawnEntityFromTable : EntityEffectBase<SpawnEntityFromTable>
{
    private const int MaxGuidebookNames = 8;

    [DataField]
    public int Number = 1;

    [DataField(required: true)]
    public EntityTableSelector EntityTable = default!;

    [DataField]
    public float Offset = 0F;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var entityTableSystem = entSys.GetEntitySystem<EntityTableSystem>();
        var spawns = entityTableSystem.ListSpawns(EntityTable)
            .Select(x => x.spawn)
            .Distinct()
            .ToList();

        if (spawns.Count == 0)
            return null;

        var names = string.Join(", ", spawns
            .Take(MaxGuidebookNames)
            .Select(x => GuidebookTextHelpers.LocalizedEntityName(prototype, x)));

        if (spawns.Count <= MaxGuidebookNames)
        {
            return Loc.GetString("entity-effect-guidebook-spawn-entity-from-table",
                ("amount", Number),
                ("entnames", names));
        }

        return Loc.GetString("entity-effect-guidebook-spawn-entity-from-table-many",
            ("amount", Number),
            ("entnames", names),
            ("count", spawns.Count - MaxGuidebookNames));
    }
}