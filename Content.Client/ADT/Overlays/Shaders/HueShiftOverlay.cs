using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.ADT.Shizophrenia;

namespace Content.Client.ADT.Overlays
{
    public sealed partial class HueShiftOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] IEntityManager _entityManager = default!;


        public override bool RequestScreenTexture => true;
        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _shader;

        public HueShiftOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index((ProtoId<ShaderPrototype>)"HueShift").InstanceUnique();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            if (!_entityManager.TryGetComponent<HueShiftComponent>(_playerManager.LocalEntity, out var hue))
                return;

            _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _shader.SetParameter("shift", hue.Shift);

            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;

            worldHandle.UseShader(_shader);
            worldHandle.DrawRect(viewport, Color.White);
            worldHandle.UseShader(null);
        }
    }
}
