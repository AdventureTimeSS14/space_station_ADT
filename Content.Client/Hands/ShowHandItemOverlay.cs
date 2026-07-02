using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Shared.ADT.MartialArts;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client.Hands
{
    public sealed class ShowHandItemOverlay : Overlay
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        private readonly SpriteSystem _sprite;

        private static readonly ResPath ComboAttackRsi =
            new ResPath("/Textures/ADT/Interface/Misc/intents.rsi");

        private HandsSystem? _hands;
        private readonly IRenderTexture _renderBackbuffer;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        public Texture? IconOverride;
        public EntityUid? EntityOverride;

        public ShowHandItemOverlay()
        {
            IoCManager.InjectDependencies(this);

            _sprite = _entMan.System<SpriteSystem>();

            _renderBackbuffer = _clyde.CreateRenderTarget(
                (64, 64),
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                new TextureSampleParameters
                {
                    Filter = true
                }, nameof(ShowHandItemOverlay));
        }

        protected override void DisposeBehavior()
        {
            base.DisposeBehavior();

            _renderBackbuffer.Dispose();
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            if (!_cfg.GetCVar(CCVars.HudHeldItemShow))
                return false;

            return base.BeforeDraw(in args);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var mousePos = _inputManager.MouseScreenPosition;

            // Offscreen
            if (mousePos.Window == WindowId.Invalid)
                return;

            var screen = args.ScreenHandle;
            var offset = _cfg.GetCVar(CCVars.HudHeldItemOffset);
            var offsetVec = new Vector2(offset, offset);

            if (IconOverride != null)
            {
                screen.DrawTexture(IconOverride, mousePos.Position - IconOverride.Size / 2 + offsetVec, Color.White.WithAlpha(0.75f));
                return;
            }

            _hands ??= _entMan.System<HandsSystem>();
            var handEntity = _hands.GetActiveHandEntity();

            // Draw combo attack type icons at cursor during combat mode
            if (_player.LocalEntity != null)
            {
                var comboEv = new GetPerformedAttackTypesEvent(null);
                _entMan.EventBus.RaiseLocalEvent(_player.LocalEntity.Value, ref comboEv);
                if (comboEv.AttackTypes is { Count: > 0 })
                {
                    var color = Color.White.WithAlpha(0.75f);
                    for (var i = 0; i < comboEv.AttackTypes.Count; i++)
                    {
                        if (!_resourceCache.TryGetResource<RSIResource>(ComboAttackRsi, out var rsiResource))
                            break;

                        var rsiActual = rsiResource.RSI;
                        if (!rsiActual.TryGetState(comboEv.AttackTypes[i].ToString().ToLower(), out var state))
                            continue;

                        var texture = state.Frame0;
                        var size = texture.Size;
                        var offsetVec2 = new Vector2(-offsetVec.X,
                            (2f * i + 1f - comboEv.AttackTypes.Count) * texture.Size.Y / 1.8f);

                        screen.DrawTextureRect(texture,
                            UIBox2.FromDimensions(mousePos.Position - size / 2 + offsetVec2, size),
                            color);
                    }
                }
            }

            if (handEntity == null || !_entMan.TryGetComponent(handEntity, out SpriteComponent? sprite))
                return;

            var halfSize = _renderBackbuffer.Size / 2;
            var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;

            screen.RenderInRenderTarget(_renderBackbuffer, () =>
            {
                screen.DrawEntity(handEntity.Value, halfSize, new Vector2(1f, 1f) * uiScale, Angle.Zero, Angle.Zero, Direction.South, sprite);
            }, Color.Transparent);

            screen.DrawTexture(_renderBackbuffer.Texture, mousePos.Position - halfSize + offsetVec, Color.White.WithAlpha(0.75f));
        }
    }
}
