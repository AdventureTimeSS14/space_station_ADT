using System.Linq;
using Content.Shared.ADT.Body.Allergies;
using Content.Shared.ADT.Body.Allergies.Prototypes;
using Content.Shared.EntityEffects;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Body;

public sealed partial class AllergicSystem : EntitySystem
{
    private List<AllergicReactionPrototype> _reactions = new();

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityEffectsSystem _effectSystem = default!;

    public void InitializeShock()
    {
        SubscribeLocalEvent<AllergicComponent, AllergyFadedEvent>(Unshock);
        _reactions = _proto.EnumeratePrototypes<AllergicReactionPrototype>().ToList();
    }

    private void Unshock(EntityUid uid, AllergicComponent allergic, ref AllergyFadedEvent ev)
    {
        allergic.NextShockEvent = TimeSpan.Zero;
    }

    private void AssignNextShockTiming(AllergicComponent allergic)
    {
        allergic.NextShockEvent = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(allergic.MinimumTimeTilNextShockEvent, allergic.MaximumTimeTilNextShockEvent));
    }

    private void UpdateShock(EntityUid uid, AllergicComponent allergic)
    {
        if (allergic.AllergyStack <= 0)
            return;

        if (allergic.NextShockEvent == TimeSpan.Zero)
        {
            AssignNextShockTiming(allergic);
            return;
        }

        if (allergic.NextShockEvent > _timing.CurTime)
            return;

        AllergicReactionPrototype? picked = null;

        foreach (var reaction in _reactions)
        {
            if (allergic.AllergyStack >= reaction.StackThreshold)
                picked = reaction;
        }

        if (picked != null)
        {
            EntityEffect effect = picked.Effects[_random.Next(0, picked.Effects.Count())];
            _effectSystem.ApplyEffect(uid, effect);
        }

        AssignNextShockTiming(allergic);
    }
}