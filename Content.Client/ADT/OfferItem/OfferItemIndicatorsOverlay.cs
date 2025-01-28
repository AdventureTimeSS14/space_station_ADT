using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client.ADT.OfferItem;

public sealed class OfferItemIndicatorsOverlay : Overlay
{
    private readonly IInputManager _inputManager;
    private readonly IEntityManager _entMan;
    private readonly IEyeManager _eye;
    private readonly OfferItemSystem _offer;

    private readonly Texture _sight;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private readonly Color _mainColor = Color.White.WithAlpha(0.3f);
    private readonly Color _strokeColor = Color.Black.WithAlpha(0.5f);
    private readonly float _scale = 0.6f;  // 1 is a little big

    public OfferItemIndicatorsOverlay(IInputManager input, IEntityManager entMan, IEyeManager eye, OfferItemSystem offerSys)
    {
        _inputManager = input;
        _entMan = entMan;
        _eye = eye;
        _offer = offerSys;

        var spriteSys = _entMan.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _sight = spriteSys.Frame0(new SpriteSpecifier.Rsi(new ResPath("/Textures/ADT/Misc/give_item.rsi"), "give_item"));
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _offer.IsInOfferMode() && base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var mousePosMap = _eye.PixelToMap(_inputManager.MouseScreenPosition);

        if (mousePosMap.MapId != args.MapId)
            return;

        var limitedScale = Math.Min(1.25f, (args.ViewportControl as Control)?.UIScale ?? 1f) * _scale;

        DrawSight(_sight, args.ScreenHandle, _inputManager.MouseScreenPosition.Position, limitedScale);
    }

    private void DrawSight(Texture sight, DrawingHandleScreen screen, Vector2 centerPos, float scale)
    {
        var sightSize = sight.Size * scale;
        var expandedSize = sightSize + new Vector2(7);

        screen.DrawTextureRect(sight, UIBox2.FromDimensions(centerPos - sightSize * 0.5f, sightSize), _strokeColor);
        screen.DrawTextureRect(sight, UIBox2.FromDimensions(centerPos - expandedSize * 0.5f, expandedSize), _mainColor);
    }
}
