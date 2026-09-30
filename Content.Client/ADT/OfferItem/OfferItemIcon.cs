using System.Numerics;
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
    private const float GapAboveSprite = 0.12f;
    private const float FallbackHeight = 0.7f;

    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IEyeManager _eye = default!;

    private readonly SpriteSystem _sprites;
    private readonly SharedTransformSystem _transform;
    private readonly SpriteView _view;

    public EntityUid Anchor { get; }

    public OfferItemIcon(EntityUid anchor)
    {
        IoCManager.InjectDependencies(this);
        Anchor = anchor;

        StyleBoxOverride = new StyleBoxFlat(Color.Black.WithAlpha(0.5f));
        MinSize = new Vector2(24, 24);
        SetSize = new Vector2(24, 24);

        _sprites = _ent.System<SpriteSystem>();
        _transform = _ent.System<SharedTransformSystem>();
        _view = new SpriteView
        {
            SetSize = new Vector2(20, 20),
            OverrideDirection = Direction.South,
            MouseFilter = MouseFilterMode.Ignore,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
        };
        AddChild(_view);
    }

    public void SetItem(EntityUid item, string tooltip)
    {
        _view.SetEntity(item);
        ToolTip = tooltip;
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

        var top = _sprites.GetLocalBounds((Anchor, sprite)).Top;
        if (top < 0.2f)
            top = FallbackHeight;

        var offset = (-_eye.CurrentEye.Rotation).ToWorldVec() * -(top + GapAboveSprite);
        var screen = _eye.WorldToScreen(_transform.GetWorldPosition(xform) + offset) / UIScale;
        LayoutContainer.SetPosition(this, new Vector2(screen.X - Size.X / 2f, screen.Y - Size.Y));
    }
}
