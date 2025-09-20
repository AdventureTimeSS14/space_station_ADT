using Content.Shared.ADT.Roulette.Components;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Roulette.Systems;

public sealed class RouletteVisualsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RouletteComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var roulette, out var sprite))
        {
            if (roulette.IsSpinning)
            {
                var visualRotation = roulette.CurrentRotation * 0.125f;
                sprite.Rotation = Angle.FromDegrees(visualRotation);
            }
        }
    }
}
