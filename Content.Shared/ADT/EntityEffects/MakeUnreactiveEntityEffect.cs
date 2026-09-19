using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects;

public sealed partial class MakeUnreactiveEntityEffect : EntityEffectBase<MakeUnreactiveEntityEffect>
{
    private static readonly ProtoId<TagPrototype> TrashTag = "Trash";

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-make-unreactive");

    public override LogImpact? Impact => LogImpact.Low;
}