using Content.Shared.ADT.Silicon.Components;
using Content.Shared.ADT.StaticTV.Components;
using Content.Shared.StatusEffect;
using Content.Shared.ADT.Anomaly.Effects.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Destructible;
using Content.Server.Chat.Systems;
using Content.Server.Chat;

namespace Content.Server.ADT.Anomaly.Effects.FakeStaticTV;
public sealed class FakeStaticTVSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NervousSourceComponent, DestructionEventArgs>(OnDestruction);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NervousSourceComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            foreach (var ent in _lookup.GetEntitiesInRange(uid, comp.NervousRange))
            {
                EnsureComp<AutoEmoteComponent>(ent);
                _autoEmote.AddEmote(ent, "NervousSream");
                if (Transform(uid).Coordinates.TryDistance(EntityManager, Transform(ent).Coordinates, out var distance)
                    && distance >= comp.NervousRange)
                {
                    _autoEmote.RemoveEmote(ent, "NervousSream");
                }
            }
        }
    }


    private void OnDestruction(EntityUid uid, NervousSourceComponent comp, DestructionEventArgs args)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(uid, comp.NervousRange))
        {
            _autoEmote.RemoveEmote(ent, "NervousSream");
        }
    }
}
