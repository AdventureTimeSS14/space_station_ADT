using Content.Shared.ADT.Silicon.Components;
using Content.Shared.ADT.StaticTV.Components;
using Content.Shared.StatusEffect;
using Content.Shared.ADT.Anomaly.Effects.Components;
using Content.Server.Body.Systems;
using Content.Server.Body.Components;
using Content.Server.Chat.Systems;
using System.Linq;
using Content.Server.Actions;
using Content.Server.Body.Systems;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Cloning;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System;

namespace Content.Server.ADT.Anomaly.Effects.FakeStaticTV;
public sealed class FakeStaticTVSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
    [Dependency] private readonly EmoteOnDamageSystem _emoteOnDamage = default!;
    public override void Initialize()
    {
        base.Initialize();

    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MadnessSourceComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            foreach (var ent in _lookup.GetEntitiesInRange(uid, comp.MadnessRange))
            {
                EnsureComp<AutoEmoteComponent>(ent);
                _autoEmote.AddEmote(ent, "NervousSream");
                if (Transform(uid).Coordinates.TryDistance(EntityManager, Transform(ent).Coordinates, out var distance)
                    && distance >= comp.MadnessRange)
                {
                    _autoEmote.RemoveEmote(ent, "NervousSream");
                }
            }
        }
    }
}
