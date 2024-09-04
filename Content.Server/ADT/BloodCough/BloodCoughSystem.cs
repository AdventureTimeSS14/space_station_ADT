using Content.Server.Chat.Systems;
using Content.Shared.ADT.BloodCough;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Content.Shared.Damage;
using Content.Server.Body.Components;
using Robust.Shared.Prototypes;
//using Content.Shared.Chemistry.Reagent.ReagentPrototype;
using Content.Shared.Chemistry.Components;
using System.Numerics;
using Content.Server.Body.Components;
using Content.Server.Botany.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Jittering;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Materials;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Robust.Server.GameObjects;
using Content.Shared.Whitelist;
using Content.Server.Body.Systems;
public sealed class BloodCoughSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;

    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodCoughComponent, DamageChangedEvent>(OnMobStateDamage);
    }

    private void OnMobStateDamage(EntityUid uid, BloodCoughComponent component, DamageChangedEvent args)
    {
        if (EntityManager.TryGetComponent<DamageableComponent>(uid, out var damageable))
        {
            var currentDamage = damageable.TotalDamage;
            if (currentDamage > 70)
            {
                //Log.Debug($"Сущность {ToPrettyString(uid)} имеет урон больше 70: {currentDamage}");
                if (TryComp<BloodCoughComponent>(uid, out var posting))
                {
                    posting.CheckCoughBlood = true;
                }
            }
            if (currentDamage <= 70)
            {
                if (TryComp<BloodCoughComponent>(uid, out var posting))
                {
                    posting.CheckCoughBlood = false;
                }
            }
        }
        else
        {
            Log.Debug($"Сущность {ToPrettyString(uid)} не имеет компонента BloodCoughComponent.");
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BloodCoughComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_time.CurTime >= comp.NextSecond)
            {
                var delay = _random.Next(comp.CoughTimeMin, comp.CoughTimeMax);
                if (comp.PostingSayDamage != null)
                {
                    if (comp.CheckCoughBlood)
                        _chat.TrySendInGameICMessage(uid, comp.PostingSayDamage, InGameICChatType.Emote, ChatTransmitRange.HideChat);

                    if (TryComp<BloodstreamComponent>(uid, out var blood))
                    {
                        //var reagentProtoId = _prototypeManager.Index<ReagentPrototype>(blood.BloodReagent);
                        //var position = transform.LocalPosition;
                        //var reagentProtoId = _bloodstreamSystem.GetEntityBloodData(uid);
                        //var position = _transform.GetMapCoordinates(uid);

                        Log.Debug($"\n====\nМестоположение: {Transform(uid).Coordinates} сущности {ToPrettyString(uid)}\nСущность {ToPrettyString(uid)} имеет {blood.BloodReagent} в своём компоненте BloodstreamComponent");

                        // _entManager.SpawnAtPosition(blood.BloodReagent, Transform(uid).Coordinates);


                        // if (_robustRandom.Prob(0.2f) && reclaimer.BloodReagent is not null)
                        // {
                        //     Solution blood = new();
                        //     blood.AddReagent(reclaimer.BloodReagent, 50);
                        // _puddleSystem.TrySpillAt(uid, blood.BloodReagent, out _);
                        // }

                        //ReagentData

                        // ReactionEntity(uid, method, proto, reagentQuantity, source);
                        // var protoId = _prototypeManager.GetPrototypeData(blood.BloodReagent).Index;

                        // _entManager.SpawnAtPosition(protoId, position);


                    }
                }

                comp.NextSecond = _time.CurTime + TimeSpan.FromSeconds(delay);
            }
        }
    }
}
