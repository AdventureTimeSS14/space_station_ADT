using Content.Shared.ADT.Areas;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Areas;

public sealed class AreasCommandSystem : EntitySystem
{
    public bool Enabled;

    public override void Update(float frameTime)
    {
        if (!Enabled)
            return;

        var areas = AllEntityQuery<AreaComponent, SpriteComponent>();
        while (areas.MoveNext(out _, out var sprite))
        {
            sprite.Visible = true;
        }
    }
}
