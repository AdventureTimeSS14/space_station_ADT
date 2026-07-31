using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.ADT.GPS.UI;

public sealed class ADTMarqueeLabel : Control
{
    private readonly Label _first;
    private readonly Label _second;

    private float _textWidth;

    private float _offset;
    private float _delay;

    public float Speed { get; set; } = 25f;

    public float Gap { get; set; } = 40f;

    public float StartDelay { get; set; } = 1.5f;

    public ADTMarqueeLabel()
    {
        RectClipContent = true;

        _first = new Label { ClipText = false };
        _second = new Label { ClipText = false, Visible = false };

        AddChild(_first);
        AddChild(_second);
    }

    public string? Text
    {
        get => _first.Text;
        set
        {
            if (_first.Text == value)
                return;

            _first.Text = value;
            _second.Text = value;

            _offset = 0f;
            _delay = StartDelay;
            InvalidateMeasure();
        }
    }

    public void AddStyleClasses(params string[] styleClasses)
    {
        foreach (var styleClass in styleClasses)
        {
            _first.AddStyleClass(styleClass);
            _second.AddStyleClass(styleClass);
        }
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var unconstrained = new Vector2(float.PositiveInfinity, availableSize.Y);

        _first.Measure(unconstrained);
        _second.Measure(unconstrained);

        _textWidth = _first.DesiredSize.X;

        return new Vector2(0f, _first.DesiredSize.Y);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var scrolling = _textWidth > finalSize.X;
        _second.Visible = scrolling;

        var x = scrolling ? -_offset : 0f;
        var size = new Vector2(_textWidth, finalSize.Y);

        _first.Arrange(UIBox2.FromDimensions(new Vector2(x, 0f), size));

        if (scrolling)
            _second.Arrange(UIBox2.FromDimensions(new Vector2(x + _textWidth + Gap, 0f), size));

        return finalSize;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_textWidth <= Size.X)
        {
            if (_offset == 0f)
                return;

            _offset = 0f;
            InvalidateArrange();
            return;
        }

        if (_delay > 0f)
        {
            _delay -= args.DeltaSeconds;
            return;
        }

        _offset += Speed * args.DeltaSeconds;

        var loop = _textWidth + Gap;
        if (_offset >= loop)
            _offset -= loop;

        InvalidateArrange();
    }
}
