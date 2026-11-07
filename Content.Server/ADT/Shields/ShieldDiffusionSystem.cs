using System.Collections.Generic;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.ADT.Shields;

public sealed partial class ShieldDiffusionSystem : EntitySystem
{
    private readonly Dictionary<(EntityUid Grid, Vector2i Tile), int> _counts = new();
    private readonly Dictionary<(EntityUid Grid, Vector2i Tile), float> _durations = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _counts.Clear();
        _durations.Clear();
    }

    public bool IsDiffused(EntityUid grid, Vector2i tile)
        => _counts.TryGetValue((grid, tile), out var count) && count > 0;

    public void Add(EntityUid grid, Vector2i tile, float duration)
    {
        var key = (grid, tile);
        _counts[key] = _counts.GetValueOrDefault(key) + 1;
        _durations[key] = duration;
    }

    public void Remove(EntityUid grid, Vector2i tile)
    {
        var key = (grid, tile);
        if (!_counts.TryGetValue(key, out var count))
            return;

        if (count <= 1)
        {
            _counts.Remove(key);
            _durations.Remove(key);
        }
        else
        {
            _counts[key] = count - 1;
        }
    }

    public float GetDuration(EntityUid grid, Vector2i tile)
        => _durations.GetValueOrDefault((grid, tile));
}