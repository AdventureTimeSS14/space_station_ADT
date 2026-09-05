using System.Numerics;
using Content.Shared.Damage.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Medical;

public sealed class ADTHealingVisualsSystem : EntitySystem
{
    private readonly Dictionary<EntityUid, (EntityUid Effect, int Count)> _activeEffects = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageableComponent, EntityTerminatingEvent>(OnTargetTerminating);
    }

    public void StartHealEffect(EntityUid target, EntProtoId? effect, TimeSpan delay)
    {
        if (effect is not { } effectId || delay <= TimeSpan.Zero)
            return;

        var count = _activeEffects.TryGetValue(target, out var entry) ? entry.Count : 0;
        if (count == 0 || !Exists(entry.Effect))
        {
            var spawned = PredictedSpawnAttachedTo(effectId, new EntityCoordinates(target, Vector2.Zero));
            _activeEffects[target] = (spawned, count + 1);
        }
        else
        {
            _activeEffects[target] = (entry.Effect, count + 1);
        }
    }

    public void StopHealEffect(EntityUid target)
    {
        if (!_activeEffects.TryGetValue(target, out var entry))
            return;

        if (entry.Count <= 1)
        {
            QueueDel(entry.Effect);
            _activeEffects.Remove(target);
        }
        else
        {
            _activeEffects[target] = (entry.Effect, entry.Count - 1);
        }
    }

    private void OnTargetTerminating(Entity<DamageableComponent> target, ref EntityTerminatingEvent args)
    {
        if (_activeEffects.Remove(target.Owner, out var entry))
            QueueDel(entry.Effect);
    }
}