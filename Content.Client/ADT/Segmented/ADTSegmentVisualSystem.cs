using System.Numerics;
using Content.Shared.ADT.Segmented.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Segmented;

public sealed class ADTSegmentVisualSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private readonly Dictionary<EntityUid, LiftState> _lifting = new();
    private readonly List<EntityUid> _finished = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTSegmentComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<ADTSegmentComponent, ComponentShutdown>(OnShutdown);

        _overlay.AddOverlay(new ADTSegmentTetherOverlay(EntityManager));
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<ADTSegmentTetherOverlay>();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_lifting.Count == 0)
            return;

        var now = _timing.RealTime;
        _finished.Clear();

        foreach (var (uid, state) in _lifting)
        {
            if (!TryComp<ADTSegmentComponent>(uid, out var segment) || !TryComp<SpriteComponent>(uid, out var sprite))
            {
                _finished.Add(uid);
                continue;
            }

            var progress = 1f;

            if (state.Duration > TimeSpan.Zero)
                progress = (float) Math.Clamp((now - state.LocalStart).TotalSeconds / state.Duration.TotalSeconds, 0, 1);

            if (progress >= 1f)
            {
                _finished.Add(uid);
                continue;
            }

            segment.VisualLift = MathF.Sin(progress * MathF.PI) * state.Height;
            _sprite.SetOffset((uid, sprite), state.BaseOffset + new Vector2(0f, segment.VisualLift));
        }

        foreach (var uid in _finished)
        {
            StopLift(uid);
        }
    }

    private void OnHandleState(Entity<ADTSegmentComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var duration = ent.Comp.StepEnd - ent.Comp.StepStart;

        if (_lifting.TryGetValue(ent, out var current))
        {
            if (current.ServerStart == ent.Comp.StepStart && current.Duration == duration)
                return;

            StopLift(ent);

            if (current.ServerStart == ent.Comp.StepStart)
                return;
        }

        if (duration <= TimeSpan.Zero || ent.Comp.StepHeight <= 0f)
            return;

        if (ent.Comp.StepEnd + duration < _timing.CurTime)
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _lifting[ent] = new LiftState
        {
            ServerStart = ent.Comp.StepStart,
            LocalStart = _timing.RealTime,
            Duration = duration,
            Height = ent.Comp.StepHeight,
            BaseOffset = sprite.Offset,
        };
    }

    private void OnShutdown(Entity<ADTSegmentComponent> ent, ref ComponentShutdown args)
    {
        _lifting.Remove(ent);
    }

    private void StopLift(EntityUid uid)
    {
        if (!_lifting.Remove(uid, out var state))
            return;

        if (TryComp<ADTSegmentComponent>(uid, out var segment))
            segment.VisualLift = 0f;

        if (TryComp<SpriteComponent>(uid, out var sprite))
            _sprite.SetOffset((uid, sprite), state.BaseOffset);
    }

    private struct LiftState
    {
        public TimeSpan ServerStart;
        public TimeSpan LocalStart;
        public TimeSpan Duration;
        public float Height;
        public Vector2 BaseOffset;
    }
}
