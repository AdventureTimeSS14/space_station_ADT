using Content.Shared.ADT.Paint;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Paint;
public sealed class PaintPoweredLightSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;

    public void TryUpdateLightColor(EntityUid uid, ColorPaintedComponent painted)
    {
        if (!TryComp<PointLightComponent>(uid, out var pointLight))
            return;

        _pointLight.SetColor(uid, painted.Color, pointLight);
    }
}
