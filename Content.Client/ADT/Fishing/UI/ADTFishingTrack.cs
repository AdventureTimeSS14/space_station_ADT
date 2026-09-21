using System.Numerics;
using Content.Shared.ADT.Fishing.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Fishing.UI;

public sealed class ADTFishingTrack : Control
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    private static readonly Color Frame = Color.FromHex("#2B1A12");
    private static readonly Color TrackTop = Color.FromHex("#3D1C0E");
    private static readonly Color TrackBottom = Color.FromHex("#120A07");
    private static readonly Color HookColor = Color.FromHex("#FF8A2B");
    private static readonly Color HookHitColor = Color.FromHex("#FFD24A");
    private static readonly Color FishColor = Color.FromHex("#FFE9A8");
    private static readonly Color BarBack = Color.FromHex("#180F0B");

    private const float Padding = 6f;
    private const int Stripes = 24;

    public EntityUid Rod;

    public event Action<bool>? OnHold;

    private float _fish = 0.5f;
    private float _hook = 0.5f;
    private float _hookSize = 0.22f;
    private float _progress = 0.4f;
    private float _glow;

    public ADTFishingTrack()
    {
        IoCManager.InjectDependencies(this);

        MouseFilter = MouseFilterMode.Stop;
        MinWidth = 180;
        MinHeight = 240;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_entMan.TryGetComponent<ADTFishingMinigameComponent>(Rod, out var comp))
            return;

        var t = Math.Clamp(args.DeltaSeconds * 16f, 0f, 1f);

        _fish = MathHelper.Lerp(_fish, comp.FishPosition, t);
        _hook = MathHelper.Lerp(_hook, comp.HookPosition, t);
        _progress = MathHelper.Lerp(_progress, comp.Progress, t);
        _hookSize = comp.HookSize;

        var onTarget = Math.Abs(comp.FishPosition - comp.HookPosition) <= comp.HookSize / 2f;
        var target = onTarget ? 1f : 0f;

        _glow = MathHelper.Lerp(_glow, target, Math.Clamp(args.DeltaSeconds * 8f, 0f, 1f));
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var box = PixelSizeBox;
        var trackWidth = Math.Max(24f, box.Width * 0.38f);
        var trackLeft = box.Left + box.Width * 0.18f;
        var track = new UIBox2(trackLeft, box.Top + Padding, trackLeft + trackWidth, box.Bottom - Padding);

        handle.DrawRect(new UIBox2(track.Left - 2f, track.Top - 2f, track.Right + 2f, track.Bottom + 2f), Frame);
        DrawGradient(handle, track, TrackTop, TrackBottom);

        var usable = track.Height;
        var hookHeight = Math.Max(10f, usable * _hookSize);
        var hookCenter = track.Bottom - usable * _hook;
        var hookTop = Math.Clamp(hookCenter - hookHeight / 2f, track.Top, track.Bottom - hookHeight);
        var hookColor = Color.InterpolateBetween(HookColor, HookHitColor, _glow);

        handle.DrawRect(new UIBox2(track.Left, hookTop, track.Right, hookTop + hookHeight), hookColor.WithAlpha(0.35f + _glow * 0.25f));
        handle.DrawRect(new UIBox2(track.Left, hookTop, track.Right, hookTop + 2f), hookColor);
        handle.DrawRect(new UIBox2(track.Left, hookTop + hookHeight - 2f, track.Right, hookTop + hookHeight), hookColor);

        var fishY = track.Bottom - usable * _fish;
        var fishRadius = Math.Max(5f, trackWidth * 0.22f);
        var fishCenter = new Vector2(track.Left + trackWidth / 2f, fishY);

        handle.DrawCircle(fishCenter, fishRadius * 1.8f, FishColor.WithAlpha(0.18f));
        handle.DrawCircle(fishCenter, fishRadius, FishColor);

        var barLeft = track.Right + box.Width * 0.12f;
        var barWidth = Math.Max(10f, box.Width * 0.14f);
        var bar = new UIBox2(barLeft, track.Top, barLeft + barWidth, track.Bottom);

        handle.DrawRect(new UIBox2(bar.Left - 2f, bar.Top - 2f, bar.Right + 2f, bar.Bottom + 2f), Frame);
        handle.DrawRect(bar, BarBack);

        var fillHeight = bar.Height * Math.Clamp(_progress, 0f, 1f);
        var fillColor = Color.InterpolateBetween(Color.FromHex("#C4372B"), Color.FromHex("#6ED06A"), _progress);

        handle.DrawRect(new UIBox2(bar.Left, bar.Bottom - fillHeight, bar.Right, bar.Bottom), fillColor);
    }

    private static void DrawGradient(DrawingHandleScreen handle, UIBox2 box, Color top, Color bottom)
    {
        var step = box.Height / Stripes;

        for (var i = 0; i < Stripes; i++)
        {
            var color = Color.InterpolateBetween(top, bottom, i / (float)(Stripes - 1));
            var stripeTop = box.Top + step * i;

            handle.DrawRect(new UIBox2(box.Left, stripeTop, box.Right, stripeTop + step + 1f), color);
        }
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        OnHold?.Invoke(true);
        args.Handle();
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        OnHold?.Invoke(false);
        args.Handle();
    }
}
