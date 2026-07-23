using Content.Shared._VG.EntityEffects;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;

namespace Content.Server._VG.EntityEffects;

public sealed partial class SexChangeSystem : EntityEffectSystem<HumanoidProfileComponent, SexChange>
{
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;

    protected override void Effect(Entity<HumanoidProfileComponent> entity, ref EntityEffectEvent<SexChange> args)
    {
        var effect = args.Effect;

        if (effect.NewSex.HasValue)
        {
            _humanoid.SetSex((entity, entity.Comp), effect.NewSex.Value);
            return;
        }

        var newSex = entity.Comp.Sex == Sex.Male ? Sex.Female : Sex.Male;
        _humanoid.SetSex((entity, entity.Comp), newSex);
    }
}