using System.Numerics;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.ADT.OfferItem;

/// <summary>
/// Small clickable item icon that follows the character offering an item.
/// </summary>
public sealed class OfferItemIcon : ContainerButton
{
    private const float HeadHeightFactor = 0.9f;
    private const float FallbackHeight = 0.7f;
    private const float PulseSpeed = 4f;
    private const int GlowLayers = 5;
    private const float GlowStep = 2f;

    private static readonly Color GiverBorderColor = Color.FromHex("#ffcc33");
    private static readonly Color ReceiverBorderColor = Color.FromHex("#7fd17f");

    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IGameTiming _timing = default!;

    private SpriteSystem _sprites;
    private SharedTransformSystem _transform;
    private SpriteView _view;
    private StyleBoxFlat _style;
    private float _pulse;

    public EntityUid Anchor { get; }

    public bool IsGiver { get; set; }

    public OfferItemIcon(EntityUid anchor)
    {
        IoCManager.InjectDependencies(this);
        Anchor = anchor;

        _style = new StyleBoxFlat(Color.Black.WithAlpha(0.5f))
        {
            BorderThickness = new Thickness(1),
            BorderColor = Color.Transparent,
        };
        StyleBoxOverride = _style;

        _sprites = _ent.System<SpriteSystem>();
        _transform = _ent.System<SharedTransformSystem>();
        _view = new SpriteView
        {
            OverrideDirection = Direction.South,
            Stretch = SpriteView.StretchMode.Fill,
            MouseFilter = MouseFilterMode.Ignore,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
        };
        AddChild(_view);
        SetIconSize(32);
    }

    public void SetItem(EntityUid item, string tooltip)
    {
        _view.SetEntity(item);
        ToolTip = tooltip;
    }

    public void SetIconSize(float size)
    {
        var box = new Vector2(size, size);
        if (SetSize == box)
            return;

        MinSize = box;
        SetSize = box;
        _view.SetSize = box - new Vector2(2, 2);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_ent.TryGetComponent(Anchor, out TransformComponent? xform)
            || !_ent.TryGetComponent(Anchor, out SpriteComponent? sprite)
            || xform.MapID != _eye.CurrentEye.Position.MapId)
        {
            Visible = false;
            return;
        }

        Visible = true;
        UpdateBorder();

        var top = GetHeadTop((Anchor, sprite));
        if (top < 0.2f)
            top = FallbackHeight;

        var offset = (-_eye.CurrentEye.Rotation).ToWorldVec() * -(top * HeadHeightFactor);
        var screen = _eye.WorldToScreen(_transform.GetWorldPosition(xform) + offset) / UIScale;
        LayoutContainer.SetPosition(this, new Vector2(screen.X - Size.X / 2f, screen.Y - Size.Y));
    }

    private void UpdateBorder()
    {
        if (IsGiver)
        {
            _pulse = 0.55f + 0.45f * MathF.Sin((float) _timing.RealTime.TotalSeconds * PulseSpeed);
            _style.BorderThickness = new Thickness(2);
            _style.BorderColor = GiverBorderColor.WithAlpha(_pulse);
            return;
        }

        _style.BorderThickness = new Thickness(1);
        _style.BorderColor = IsHovered ? ReceiverBorderColor : ReceiverBorderColor.WithAlpha(0.6f);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (IsGiver)
        {
            var box = (UIBox2) PixelSizeBox;
            for (var i = GlowLayers; i >= 1; i--)
            {
                var grow = i * GlowStep * UIScale;
                var alpha = _pulse * 0.35f * (1f - (i - 1f) / GlowLayers);
                handle.DrawRect(new UIBox2(box.TopLeft - new Vector2(grow), box.BottomRight + new Vector2(grow)), GiverBorderColor.WithAlpha(alpha / GlowLayers * 2f));
            }
        }

        base.Draw(handle);
    }

    /// <summary>
    /// Top of the head layer for humanoids, so big layers like wings, tails or long hair do not push the icon up.
    /// Falls back to the whole sprite bounds.
    /// </summary>
    private float GetHeadTop(Entity<SpriteComponent> sprite)
    {
        if (_sprites.LayerMapTryGet(sprite.AsNullable(), HumanoidVisualLayers.Head, out var index, false)
            && sprite.Comp[index] is SpriteComponent.Layer { Visible: true } head)
        {
            return _sprites.GetLocalBounds(head).Top * MathF.Abs(sprite.Comp.Scale.Y);
        }

        return _sprites.GetLocalBounds(sprite).Top;
    }
}
