using Content.Shared.ADT.Atmos.Miasma;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class Embalm : EntityEffectBase<Embalm>
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override void RaiseEvent(
        EntityUid uid,
        IEntityEffectRaiser raiser,
        float scale,
        EntityUid? user)
    {
        _entMan.EnsureComponent<EmbalmedComponent>(uid);
    }

    public override string? EntityEffectGuidebookText(
        IPrototypeManager prototype,
        IEntitySystemManager entSys)
    {
        return Loc.GetString(
            "reagent-effect-guidebook-embalm",
            ("chance", Probability));
    }
}
