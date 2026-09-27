using System.Numerics;
using Content.Shared.ADT.Chat;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.ADT.UserInterface.Controls;

public sealed class ADTD20RollControl : TextureRect
{
    [Dependency] private readonly IEntitySystemManager _entitySystems = default!;

    public const string DiceRsiPath = "Objects/Fun/dice.rsi";
    private const int SwapCount = 18;
    private const float LinePadding = 1f;

    private readonly Texture[] _faces = new Texture[SharedADTD20Emote.Faces];

    private readonly int _result;
    private readonly float _size;
    private readonly float _rollDuration;
    private readonly float _holdDuration;
    private readonly float _fadeDuration;

    private float _elapsed;
    private int _lastSwap = -1;
    private bool _finished;

    private bool _lineFit;
    private float _drawOffsetY;

    public event Action? RollFinished;

    public ADTD20RollControl(int result, float size, float holdDuration = 0f, float fadeDuration = 0f)
    {
        IoCManager.InjectDependencies(this);

        _result = Math.Clamp(result, 1, SharedADTD20Emote.Faces);
        _size = size;
        _rollDuration = (float) SharedADTD20Emote.RollDuration.TotalSeconds;
        _holdDuration = holdDuration;
        _fadeDuration = fadeDuration;

        var sprites = _entitySystems.GetEntitySystem<SpriteSystem>();

        for (var i = 0; i < SharedADTD20Emote.Faces; i++)
        {
            _faces[i] = GetFace(sprites, i + 1);
        }

        Texture = _faces[FaceForSwap(0)];
        Stretch = StretchMode.Scale;
        SetSize = new Vector2(size, size);
        MouseFilter = MouseFilterMode.Ignore;
    }

    public void FitToTextLine(float lineHeight)
    {
        _lineFit = true;
        _drawOffsetY = -(_size - lineHeight) / 2f;

        SetSize = new Vector2(_size + LinePadding * 2f, lineHeight);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (!_lineFit)
        {
            base.Draw(handle);
            return;
        }

        if (Texture == null)
            return;

        var position = new Vector2(LinePadding, _drawOffsetY) * UIScale;
        var size = new Vector2(_size, _size) * UIScale;

        handle.DrawTextureRect(Texture, UIBox2.FromDimensions(position, size));
    }

    public static Texture GetFace(SpriteSystem sprites, int face)
    {
        return sprites.Frame0(new SpriteSpecifier.Rsi(new ResPath(DiceRsiPath), $"d20_{face}"));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_finished)
            return;

        _elapsed += args.DeltaSeconds;

        if (_elapsed < _rollDuration)
        {
            RollFace();
            return;
        }

        Texture = _faces[_result - 1];

        var afterRoll = _elapsed - _rollDuration;
        if (afterRoll < _holdDuration)
            return;

        if (_fadeDuration > 0f)
        {
            var fade = (afterRoll - _holdDuration) / _fadeDuration;
            if (fade < 1f)
            {
                Modulate = Color.White.WithAlpha(1f - fade);
                return;
            }
        }

        _finished = true;
        RollFinished?.Invoke();
    }

    private void RollFace()
    {
        var progress = _elapsed / _rollDuration;
        var eased = 1f - MathF.Pow(1f - progress, 3f);
        var swap = Math.Min((int) (eased * SwapCount), SwapCount - 1);

        if (swap == _lastSwap)
            return;

        _lastSwap = swap;
        Texture = _faces[FaceForSwap(swap)];
    }

    private int FaceForSwap(int swap)
    {
        var stepsLeft = SwapCount - 1 - swap;
        var face = (_result - 1 - stepsLeft) % SharedADTD20Emote.Faces;

        if (face < 0)
            face += SharedADTD20Emote.Faces;

        return face;
    }
}
