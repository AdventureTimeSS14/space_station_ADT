using Content.Server.Chat.Systems;
using Content.Shared.ADT.BloodCough;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Content.Shared.Damage;
using Content.Server.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.ADT.Silicon.Components;
using Robust.Shared.Prototypes;


public sealed class BloodCoughSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodCoughComponent, DamageChangedEvent>(OnMobStateDamage);
    }

    private void OnMobStateDamage(EntityUid uid, BloodCoughComponent component, DamageChangedEvent args)
    {
        if (EntityManager.TryGetComponent<DamageableComponent>(uid, out var damageable))
        {
            var damagePerGroup = damageable.Damage.GetDamagePerGroup(_prototypeManager);
            if (damagePerGroup.TryGetValue("Brute", out var bruteDamage))
            {
                if (bruteDamage > 70)
                {
                    if (TryComp<BloodCoughComponent>(uid, out var posting))
                    {
                        posting.CheckCoughBlood = true;
                    }
                }
                if (bruteDamage <= 70)
                {
                    if (TryComp<BloodCoughComponent>(uid, out var posting))
                    {
                        posting.CheckCoughBlood = false;
                    }
                }
            }
        }
        else
        {
            Log.Debug($"Сущность {ToPrettyString(uid)} не имеет компонента DamageableComponent.");
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BloodCoughComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobState.IsAlive(uid))
            {
                if (_time.CurTime >= comp.NextSecond)
                {
                    var delay = _random.Next(comp.CoughTimeMin, comp.CoughTimeMax);
                    if (comp.PostingSayDamage != null)
                    {
                        if (comp.CheckCoughBlood)
                        {
                            _chat.TrySendInGameICMessage(uid, Loc.GetString(comp.PostingSayDamage), InGameICChatType.Emote, ChatTransmitRange.HideChat);
                            if (comp.CheckCoughBlood && TryComp<BloodstreamComponent>(uid, out var bloodId) && !TryComp<SiliconComponent>(uid, out var _)) //лютейший костыль, тут проверка, потому что у кпб в крови ВОДА и при кашле он воду спавнит. поэтому сущности с SiliconComponent игнорим
                            {
                                Solution blood = new();
                                blood.AddReagent(bloodId.BloodReagent, 1);
                                _puddleSystem.TrySpillAt(uid, blood, out _, sound: false);
                            }
                        }
                    }
                    comp.NextSecond = _time.CurTime + TimeSpan.FromSeconds(delay);
                }
            }
        }
    }
}
