using Content.Goobstation.Shared.SpecialPassives.BoostedImmunity.Components;

namespace Content.Goobstation.Shared.SpecialPassives.BoostedImmunity;

public sealed class BoostedImmunitySystem : SharedBoostedImmunitySystem
{
    // ADT-Tweak
    protected override void RemoveDisabilities(Entity<BoostedImmunityComponent> ent) { }

    protected override void RemoveAlienEmbryo(Entity<BoostedImmunityComponent> ent) { }

    protected override void RemoveDiseases(Entity<BoostedImmunityComponent> ent) { }
}
