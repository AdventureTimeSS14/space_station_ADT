using Content.Server.Chat.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.ADT.Silicon.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.BloodCough;

public sealed class BloodCoughSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodCoughComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<BloodCoughComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<BloodCoughComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            // Если не кашляем или ещё не время - пропускаем
            if (!comp.CheckCoughBlood || comp.NextCough > curTime)
                continue;

            // Проверяем, что сущность жива
            if (!_mobState.IsAlive(uid))
                continue;

            // Проверяем, что урон все еще в нужном диапазоне
            if (!TryComp<DamageableComponent>(uid, out var damageable))
                continue;

            var damagePerGroup = _damageable.GetDamagePerGroup((uid, damageable));
            if (!damagePerGroup.TryGetValue("Brute", out var bruteDamage))
                continue;

            if (bruteDamage < 70 || bruteDamage > 100)
            {
                comp.CheckCoughBlood = false;
                continue;
            }

            // Выполняем кашель
            PerformCough(uid, comp);

            // Устанавливаем следующий интервал
            var nextDelay = _random.Next(comp.CoughTimeMin, comp.CoughTimeMax);
            comp.NextCough += TimeSpan.FromSeconds(nextDelay);
        }
    }

    private void OnMapInit(EntityUid uid, BloodCoughComponent component, MapInitEvent args)
    {
        // Инициализация при появлении на карте
        CheckAndUpdateCoughState(uid, component);
    }

    private void OnDamageChanged(EntityUid uid, BloodCoughComponent component, DamageChangedEvent args)
    {
        CheckAndUpdateCoughState(uid, component);
    }

    private void CheckAndUpdateCoughState(EntityUid uid, BloodCoughComponent component)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
        {
            return;
        }

        var damagePerGroup = _damageable.GetDamagePerGroup((uid, damageable));
        if (!damagePerGroup.TryGetValue("Brute", out var bruteDamage))
            return;

        var shouldCough = bruteDamage >= 70 && bruteDamage <= 100;

        if (component.CheckCoughBlood != shouldCough)
        {
            component.CheckCoughBlood = shouldCough;

            if (shouldCough)
            {
                // Запускаем кашель с первой задержкой
                var initialDelay = _random.Next(component.CoughTimeMin, component.CoughTimeMax);
                component.NextCough = _timing.CurTime + TimeSpan.FromSeconds(initialDelay);
            }
            else
            {
                // Сбрасываем время следующего кашля
                component.NextCough = TimeSpan.Zero;
            }
        }
    }

    private void PerformCough(EntityUid uid, BloodCoughComponent component)
    {
        if (!string.IsNullOrEmpty(component.PostingSayDamage))
        {
            _chat.TrySendInGameICMessage(uid, Loc.GetString(component.PostingSayDamage), InGameICChatType.Emote, ChatTransmitRange.HideChat);

            if (TryComp<BloodstreamComponent>(uid, out var bloodstream) &&
                !TryComp<SiliconComponent>(uid, out var _))
            {
                var bloodReagents = bloodstream.BloodReferenceSolution.Contents;
                var bloodReagentId = bloodReagents.Count > 0
                    ? bloodReagents[0].Reagent.Prototype
                    : "Blood";

                var solution = new Solution();
                solution.AddReagent(bloodReagentId, 1);
                _puddleSystem.TrySpillAt(uid, solution, out _, sound: false);
            }
        }
    }
}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/
