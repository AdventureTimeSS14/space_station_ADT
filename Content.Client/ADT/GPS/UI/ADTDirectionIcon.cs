using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client.ADT.GPS.UI;

public sealed class ADTDirectionIcon : TextureRect
{
    private const string ArrowTexturePath = "/Textures/Interface/VerbIcons/drop.svg.192dpi.png";
    private const string HereTexturePath = "/Textures/Interface/VerbIcons/dot.svg.192dpi.png";
    private const string UnknownTexturePath = "/Textures/Interface/VerbIcons/information.svg.192dpi.png";

    private readonly Texture _arrowTexture;
    private readonly Texture _hereTexture;
    private readonly Texture _unknownTexture;

    private Angle? _rotation;
    private bool _snap;
    private float _minDistance;

    public Angle? Rotation
    {
        get => _rotation;
        private set
        {
            _rotation = value;
            Texture = value == null ? _unknownTexture : _arrowTexture;
        }
    }

    public ADTDirectionIcon()
    {
        var resCache = IoCManager.Resolve<IResourceCache>();
        _arrowTexture = resCache.GetResource<TextureResource>(ArrowTexturePath);
        _hereTexture = resCache.GetResource<TextureResource>(HereTexturePath);
        _unknownTexture = resCache.GetResource<TextureResource>(UnknownTexturePath);

        Stretch = StretchMode.KeepAspectCentered;
        Texture = _unknownTexture;
    }

    public ADTDirectionIcon(bool snap = true, float minDistance = 0.1f) : this()
    {
        _snap = snap;
        _minDistance = minDistance;
    }

    public void UpdateDirection(Direction direction)
    {
        Rotation = direction.ToAngle();
    }

    public void UpdateDirection(Vector2 direction, Angle relativeAngle)
    {
        if (direction.EqualsApprox(Vector2.Zero, _minDistance))
        {
            _rotation = null;
            Texture = _hereTexture;
            return;
        }

        var rotation = direction.ToWorldAngle() - relativeAngle;
        Rotation = _snap ? rotation.GetDir().ToAngle() : rotation;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_rotation != null)
        {
            var offset = (-_rotation.Value).RotateVec(Size * UIScale / 2) - Size * UIScale / 2;
            handle.SetTransform(Matrix3Helpers.CreateTransform(GlobalPixelPosition - offset, -_rotation.Value));
        }

        base.Draw(handle);
    }
}
