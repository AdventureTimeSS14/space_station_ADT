using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Cooking;
using Content.Shared.ADT.Cooking.Components;

namespace Content.Client.ADT.Cooking;

public sealed partial class ADTCookingSystem
{
    private const float FlipDuration = 0.5f;
    private const float FlipHeight = 0.18f;

    private readonly Dictionary<EntityUid, TimeSpan> _flips = new();

    private void InitializeFlip()
    {
        SubscribeNetworkEvent<ADTCookwareFlippedEvent>(OnCookwareFlipped);
    }

    private void OnCookwareFlipped(ADTCookwareFlippedEvent ev)
    {
        var uid = GetEntity(ev.Cookware);
        if (!TryComp<ADTCookwareComponent>(uid, out var comp))
            return;

        _flips[uid] = _timing.RealTime;
        RefreshCookware((uid, comp));
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        SpawnQueuedPreviews();

        if (_flips.Count == 0)
            return;

        foreach (var uid in _flips.Keys.ToList())
        {
            if (!TryComp<ADTCookwareComponent>(uid, out var comp))
            {
                _flips.Remove(uid);
                continue;
            }

            if (GetFlipProgress(uid) == null)
                _flips.Remove(uid);

            RefreshCookware((uid, comp));
        }
    }

    private float? GetFlipProgress(EntityUid uid)
    {
        if (!_flips.TryGetValue(uid, out var start))
            return null;

        var t = (float) (_timing.RealTime - start).TotalSeconds / FlipDuration;
        if (t < 0f || t >= 1f)
            return null;

        return t;
    }

    private void ApplyFlip(EntityUid uid, ref Vector2 position, ref Vector2 scale)
    {
        if (GetFlipProgress(uid) is not { } t)
            return;

        position.Y += FlipHeight * 4f * t * (1f - t);
        scale.Y *= MathF.Cos(MathF.PI * 2f * t);
    }
}
