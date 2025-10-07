using Content.Server.Chat.Systems;
using Robust.Shared.Random;
using Content.Shared.Damage;
using Content.Server.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.ADT.Silicon.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Chat;
using System.Threading;

namespace Content.Server.ADT.BloodCough;

public sealed class BloodCoughSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodCoughComponent, DamageChangedEvent>(OnMobStateDamage);
        SubscribeLocalEvent<BloodCoughComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<BloodCoughComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<BloodCoughComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnComponentStartup(EntityUid uid, BloodCoughComponent component, ComponentStartup args)
    {
        // Проверяем текущий урон и запускаем таймер если нужно
        CheckAndUpdateCoughState(uid, component);
    }

    private void OnComponentShutdown(EntityUid uid, BloodCoughComponent component, ComponentShutdown args)
    {
        // Отменяем таймер при удалении компонента
        if (component.TokenSource != null)
        {
            component.TokenSource.Cancel();
            component.TokenSource.Dispose();
            component.TokenSource = null;
        }
    }

    private void OnAfterAutoHandleState(EntityUid uid, BloodCoughComponent component, ref AfterAutoHandleStateEvent args)
    {
        // Обновляем состояние кашля при изменении компонента
        CheckAndUpdateCoughState(uid, component);
    }

    private void OnMobStateDamage(EntityUid uid, BloodCoughComponent component, DamageChangedEvent args)
    {
        CheckAndUpdateCoughState(uid, component);
    }

    private void CheckAndUpdateCoughState(EntityUid uid, BloodCoughComponent component)
    {
        if (!EntityManager.TryGetComponent<DamageableComponent>(uid, out var damageable))
        {
            Log.Debug($"Сущность {ToPrettyString(uid)} не имеет компонента DamageableComponent.");
            return;
        }

        var damagePerGroup = damageable.Damage.GetDamagePerGroup(_prototypeManager);
        if (!damagePerGroup.TryGetValue("Brute", out var bruteDamage))
            return;

        var shouldCough = bruteDamage >= 70 && bruteDamage <= 100;

        if (component.CheckCoughBlood != shouldCough)
        {
            component.CheckCoughBlood = shouldCough;

            if (shouldCough)
            {
                StartCoughTimer(uid, component);
            }
            else
            {
                StopCoughTimer(uid, component);
            }
        }
    }

    private void StartCoughTimer(EntityUid uid, BloodCoughComponent component)
    {
        // Отменяем предыдущий таймер если он существует
        component.TokenSource?.Cancel();
        component.TokenSource?.Dispose();
        component.TokenSource = new CancellationTokenSource();

        var delay = _random.Next(component.CoughTimeMin, component.CoughTimeMax);

        // Запускаем повторяющийся таймер
        uid.SpawnRepeatingTimer(TimeSpan.FromSeconds(delay), () => OnCoughTimerFired(uid, component), component.TokenSource.Token);
    }

    private void StopCoughTimer(EntityUid uid, BloodCoughComponent component)
    {
        if (component.TokenSource != null)
        {
            component.TokenSource.Cancel();
            component.TokenSource.Dispose();
            component.TokenSource = null;
        }
    }

    private void OnCoughTimerFired(EntityUid uid, BloodCoughComponent component)
    {
        // Проверяем, что компонент все еще существует
        if (!HasComp<BloodCoughComponent>(uid))
            return;

        // Проверяем, что сущность жива
        if (!_mobState.IsAlive(uid))
            return;

        // Проверяем, что урон все еще в нужном диапазоне
        if (!EntityManager.TryGetComponent<DamageableComponent>(uid, out var damageable))
            return;

        var damagePerGroup = damageable.Damage.GetDamagePerGroup(_prototypeManager);
        if (!damagePerGroup.TryGetValue("Brute", out var bruteDamage))
            return;

        if (bruteDamage < 70 || bruteDamage > 100)
        {
            component.CheckCoughBlood = false;
            StopCoughTimer(uid, component);
            return;
        }

        PerformCough(uid, component);
    }

    private void PerformCough(EntityUid uid, BloodCoughComponent component)
    {
        if (component.PostingSayDamage != null)
        {
            _chat.TrySendInGameICMessage(uid, Loc.GetString(component.PostingSayDamage), InGameICChatType.Emote, ChatTransmitRange.HideChat);

            if (TryComp<BloodstreamComponent>(uid, out var bloodId) &&
                !TryComp<SiliconComponent>(uid, out var _))
            {
                Solution blood = new();
                blood.AddReagent(bloodId.BloodReagent, 1);
                _puddleSystem.TrySpillAt(uid, blood, out _, sound: false);
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
