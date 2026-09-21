using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Client.ADT.UserInterface.Controls;

[Virtual]
public class StaticSpriteView : Control
{
    protected SpriteSystem? SpriteSystem;
    private SharedTransformSystem? _transform;
    private IClyde _clyde = default!;
    protected readonly IEntityManager EntMan;

    private IRenderTexture? _snapshot;

    [ViewVariables]
    public SpriteComponent? Sprite => Entity?.Comp1;

    [ViewVariables]
    public Entity<SpriteComponent, TransformComponent>? Entity { get; private set; }

    [ViewVariables]
    public NetEntity? NetEnt { get; private set; }

    public bool IsVisible { get; set; } = true;
 
    /// <summary>
    /// This field configures automatic scaling of the sprite. This automatic scaling is done before
    /// applying the explicitly set scale <see cref="SunriseStaticSpriteView.Scale"/>.
    /// </summary>
    public StretchMode Stretch
    {
        get => _stretch;
        set
        {
            _stretch = value;
            InvalidateSnapshot();
        }
    }
    private StretchMode _stretch = StretchMode.Fit;

    public enum StretchMode
    {
        /// <summary>
        /// Don't automatically scale the sprite. The sprite can still be scaled via <see cref="SunriseStaticSpriteView.Scale"/>
        /// </summary>
        None,

        /// <summary>
        /// Scales the sprite down so that it fits within the control. Does not scale the sprite up. Keeps the same
        /// aspect ratio. This automatic scaling is done before applying <see cref="SunriseStaticSpriteView.Scale"/>.
        /// </summary>
        Fit,

        /// <summary>
        /// Scale the sprite up or down so that it fills the whole control. Keeps the same aspect ratio. This
        /// automatic scaling is done before applying <see cref="SunriseStaticSpriteView.Scale"/>.
        /// </summary>
        Fill
    }

    /// <summary>
    /// Overrides the direction used to render the sprite.
    /// </summary>
    /// <remarks>
    /// If null, the world space orientation of the entity will be used. Otherwise the specified direction will be
    /// used.
    /// </remarks>
    public Direction? OverrideDirection
    {
        get => _overrideDirection;
        set
        {
            _overrideDirection = value;
            InvalidateSnapshot();
        }
    }
    private Direction? _overrideDirection;

    private Vector2 _scale = Vector2.One;
    private Angle _eyeRotation = Angle.Zero;
    private Angle? _worldRotation = Angle.Zero;
    private Vector2 _spriteSize;

    public Vector2 Offset
    {
        get => _offset;
        set
        {
            _offset = value;
            InvalidateSnapshot();
        }
    }
    private Vector2 _offset;

    public bool SpriteOffset
    {
        get => _spriteOffset;
        set
        {
            _spriteOffset = value;
            InvalidateSnapshot();
        }
    }
    private bool _spriteOffset;

    public Angle EyeRotation
    {
        get => _eyeRotation;
        set
        {
            _eyeRotation = value;
            InvalidateMeasure();
            InvalidateSnapshot();
        }
    }

    /// <summary>
    /// Used to override the entity's world rotation. Note that the desired size of the control will not
    /// automatically get updated as the entity's world rotation changes.
    /// </summary>
    public Angle? WorldRotation
    {
        get => _worldRotation;
        set
        {
            _worldRotation = value;
            InvalidateMeasure();
            InvalidateSnapshot();
        }
    }

    /// <summary>
    /// Scale to apply when rendering the sprite. This is separate from the sprite's scale.
    /// </summary>
    public Vector2 Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            InvalidateMeasure();
            InvalidateSnapshot();
        }
    }

    public StaticSpriteView()
    {
        IoCManager.Resolve(ref EntMan);
        IoCManager.Resolve(ref _clyde);
        RectClipContent = true;
    }

    public StaticSpriteView(IEntityManager entMan)
    {
        EntMan = entMan;
        _clyde = IoCManager.Resolve<IClyde>();
        RectClipContent = true;
    }

    public StaticSpriteView(EntityUid? uid, IEntityManager entMan)
    {
        EntMan = entMan;
        _clyde = IoCManager.Resolve<IClyde>();
        RectClipContent = true;
        SetEntity(uid);
    }

    public StaticSpriteView(NetEntity uid, IEntityManager entMan)
    {
        EntMan = entMan;
        _clyde = IoCManager.Resolve<IClyde>();
        RectClipContent = true;
        SetEntity(uid);
    }

    public void SetEntity(NetEntity netEnt)
    {
        if (netEnt == NetEnt)
            return;

        // Подписаться на событие появления сущности
        Entity = null;
        NetEnt = netEnt;
        InvalidateSnapshot();
    }

    public void SetEntity(EntityUid? uid)
    {
        if (Entity?.Owner == uid)
            return;

        if (!EntMan.TryGetComponent(uid, out SpriteComponent? sprite) ||
            !EntMan.TryGetComponent(uid, out TransformComponent? xform))
        {
            Entity = null;
            NetEnt = null;
            InvalidateSnapshot();
            return;
        }

        Entity = new(uid.Value, sprite, xform);
        NetEnt = EntMan.GetNetEntity(uid);
        InvalidateSnapshot();
    }

    private void InvalidateSnapshot()
    {
        _snapshot?.Dispose();
        _snapshot = null;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        InvalidateSnapshot();
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        // TODO Make this get called when sprite bounds/properties update?
        UpdateSize();
        var setSize = SetSize;
        if (!float.IsNaN(setSize.X) && !float.IsNaN(setSize.Y))
            return setSize;

        return _spriteSize;
    }

    private void UpdateSize()
    {
        if (!ResolveEntity(out _, out var sprite, out _))
            return;

        var spriteBox = sprite.CalculateRotatedBoundingBox(default, _worldRotation ?? Angle.Zero, _eyeRotation)
            .CalcBoundingBox();

        if (!SpriteOffset)
            spriteBox = spriteBox.Translated(-spriteBox.Center);

        // Scale the box (including any offset);
        var scale = _scale * EyeManager.PixelsPerMeter;
        var bl = spriteBox.BottomLeft * scale;
        var tr = spriteBox.TopRight * scale;

        // This view will be centered on (0,0). If the sprite was shifted by (1,2) the actual size of the control
        // would need to be at least (2,4).
        tr = Vector2.Max(tr, Vector2.Zero);
        bl = Vector2.Min(bl, Vector2.Zero);
        tr = Vector2.Max(tr, -bl);
        bl = Vector2.Min(bl, -tr);
        var box = new Box2(bl, tr);

        DebugTools.Assert(box.Contains(Vector2.Zero));
        DebugTools.Assert(box.TopLeft.EqualsApprox(-box.BottomRight));

        if (_worldRotation != null && _eyeRotation == Angle.Zero) // TODO This shouldn't need to be here, but I just give up at this point I am going fucking insane looking at rotating blobs of pixels. I doubt anyone will ever even use rotated sprite views.?
        {
            _spriteSize = box.Size;
            return;
        }

        // Size does not auto-update with world rotation. So if it is not fixed by _worldRotation we will just take
        // the maximum possible size.
        var size = box.Size;
        var longestSide = MathF.Max(size.X, size.Y);
        var longestRotatedSide = Math.Max(longestSide, (size.X + size.Y) / MathF.Sqrt(2));
        _spriteSize = new Vector2(longestRotatedSide, longestRotatedSide);
    }

    protected override void Draw(IRenderHandle renderHandle)
    {
        if (_snapshot != null)
        {
            renderHandle.DrawingHandleScreen.DrawTextureRect(_snapshot.Texture,
                new UIBox2(0, 0, PixelSize.X, PixelSize.Y), Modulate * ActualModulateSelf);
            return;
        }

        if (!ResolveEntity(out var uid, out var sprite, out var xform))
            return;

        SpriteSystem ??= EntMan.System<SpriteSystem>();
        _transform ??= EntMan.System<TransformSystem>();
        SpriteSystem.ForceUpdate(uid);

        var pixelSize = PixelSize;
        if (pixelSize.X <= 0 || pixelSize.Y <= 0)
            return;

        if (_spriteSize == Vector2.Zero)
            UpdateSize();

        var stretchVec = Stretch switch
        {
            StretchMode.Fit => Vector2.Min(Size / _spriteSize, Vector2.One),
            StretchMode.Fill => Size / _spriteSize,
            _ => Vector2.One,
        };
        var stretch = MathF.Min(stretchVec.X, stretchVec.Y);

        var offset = SpriteOffset
            ? Vector2.Zero
            : -(-_eyeRotation).RotateVec(sprite.Offset * _scale) * new Vector2(1, -1) * EyeManager.PixelsPerMeter;

        var position = pixelSize / 2 + offset * stretch * UIScale + Offset * UIScale;
        var scale = Scale * UIScale * stretch;

        _snapshot = _clyde.CreateRenderTarget(new Vector2i((int) pixelSize.X, (int) pixelSize.Y),
            RenderTargetColorFormat.Rgba8Srgb, name: "StaticSpriteView");

        renderHandle.RenderInRenderTarget(_snapshot, () =>
        {
            renderHandle.DrawEntity(uid, position, scale, _worldRotation, _eyeRotation, OverrideDirection, sprite, xform, _transform);
        }, Color.Transparent);

        renderHandle.DrawingHandleScreen.DrawTextureRect(_snapshot.Texture,
            new UIBox2(0, 0, pixelSize.X, pixelSize.Y));
    }

    private bool ResolveEntity(
        out EntityUid uid,
        [NotNullWhen(true)] out SpriteComponent? sprite,
        [NotNullWhen(true)] out TransformComponent? xform)
    {
        if (NetEnt != null && Entity == null && EntMan.TryGetEntity(NetEnt, out var ent))
            SetEntity(ent);

        if (Entity != null)
        {
            (uid, sprite, xform) = Entity.Value;
            return !EntMan.Deleted(uid);
        }

        sprite = null;
        xform = null;
        uid = default;
        return false;
    }
}