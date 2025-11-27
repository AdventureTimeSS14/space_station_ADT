using Content.Shared.Nutrition.Components;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed class SharedHungerSystem : EntitySystem
{
    [Dependency] private readonly HungerSystem _hunger = null!;

    /// <summary>
    /// A check that returns if the entity is below a hunger threshold.
    /// </summary>
    public bool IsHungerAboveState(EntityUid uid, HungerThreshold threshold, float? food = null, HungerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false; // It's never going to go hungry, so it's probably fine to assume that it's not... you know, hungry.

        return _hunger.GetHungerThreshold(comp, food) > threshold;
    }
}
