using Content.Shared.ADT.Heretic.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.ADT.Heretic;

public sealed partial class XRayVisionSystem : SharedXRayVisionSystem
{
    [Dependency] private ILightManager _light = default!;
    [Dependency] private IPlayerManager _player = default!;

    protected override void DrawLight(EntityUid uid, bool value)
    {
        base.DrawLight(uid, value);

        if (_player.LocalEntity != uid)
            return;

        _light.DrawLighting = value;
    }
}
