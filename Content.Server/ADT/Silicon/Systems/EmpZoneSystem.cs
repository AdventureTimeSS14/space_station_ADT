using Content.Shared.ADT.Silicon.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Mind;
using Content.Shared.Physics;
using Content.Shared.ADT.Silicon.Systems;
using Content.Shared.ADT.Silicon.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Map;
using Robust.Shared.Log;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;
using System.Numerics;
using Content.Shared.Mind.Components;

namespace Content.Server.ADT.Silicon.Systems;

/// <summary>
/// Система для обработки зон ЭМИ эффекта с переменной интенсивностью.
/// </summary>
public sealed class EmpZoneSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ILogManager _log = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpZoneComponent, ComponentStartup>(OnEmpZoneStartup);
        SubscribeLocalEvent<EmpZoneComponent, ComponentShutdown>(OnEmpZoneShutdown);
        SubscribeLocalEvent<EmpZoneComponent, StartCollideEvent>(OnEmpZoneEnter);
        SubscribeLocalEvent<EmpZoneComponent, EndCollideEvent>(OnEmpZoneExit);


    }

    private void OnEmpZoneStartup(EntityUid uid, EmpZoneComponent component, ComponentStartup args)
    {
        if (!component.Enabled)
            return;

        Logger.Info($"EmpZone startup: {uid}, radius: {component.Radius}");
        SetupEmpZone(uid, component);
    }

    private void OnEmpZoneShutdown(EntityUid uid, EmpZoneComponent component, ComponentShutdown args)
    {
        // Убираем эффект со всех игроков в зоне при удалении компонента
        var query = EntityQueryEnumerator<StatusEffectsComponent>();
        while (query.MoveNext(out var playerUid, out var statusComp))
        {
            if (HasComp<MindContainerComponent>(playerUid))
            {
                _status.TryRemoveStatusEffect(playerUid, "SeeingStatic", statusComp);
            }
        }
    }

    private void OnEmpZoneEnter(EntityUid uid, EmpZoneComponent component, ref StartCollideEvent args)
    {
        if (!component.Enabled)
            return;

        if (args.OurFixtureId != component.FixtureId)
            return;

        Logger.Info($"EmpZone collision: {args.OtherEntity} entered zone {uid}");

        // Проверяем, что это игрок (MindContainerComponent)
        if (!HasComp<MindContainerComponent>(args.OtherEntity))
        {
            Logger.Debug($"Entity {args.OtherEntity} has no MindContainerComponent, skipping");
            return;
        }

        // Проверяем, есть ли у игрока StatusEffectsComponent
        if (!HasComp<StatusEffectsComponent>(args.OtherEntity))
        {
            Logger.Warning($"Player {args.OtherEntity} has no StatusEffectsComponent, cannot apply EMP effect");
            return;
        }

        Logger.Info($"Player {args.OtherEntity} has all required components, applying EMP effect");
        ApplyEmpEffect(args.OtherEntity, uid, component);
    }

    private void OnEmpZoneExit(EntityUid uid, EmpZoneComponent component, ref EndCollideEvent args)
    {
        if (args.OurFixtureId != component.FixtureId)
            return;

        if (!HasComp<MindContainerComponent>(args.OtherEntity))
            return;

        RemoveEmpEffect(args.OtherEntity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmpZoneComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.Enabled)
                continue;

            // Обновляем интенсивность для всех игроков с эффектом в этой зоне
            UpdateZoneIntensities(uid, component);
        }
    }

    /// <summary>
    /// Оптимизированно обновляет интенсивность для всех игроков в конкретной зоне.
    /// </summary>
    private void UpdateZoneIntensities(EntityUid zoneUid, EmpZoneComponent component)
    {
        var zonePos = _transform.GetWorldPosition(zoneUid);

        // Ищем только игроков с активным эффектом ЭМИ
        var playerQuery = EntityQueryEnumerator<EmpInterferenceComponent, TransformComponent>();
        while (playerQuery.MoveNext(out var playerUid, out var interferenceComp, out var transformComp))
        {
            var playerPos = transformComp.WorldPosition;
            var distance = Vector2.Distance(playerPos, zonePos);

            // Если игрок в зоне, обновляем интенсивность
            if (distance <= component.Radius)
            {
                var intensity = CalculateIntensity(distance, component);
                if (Math.Abs(interferenceComp.Multiplier - intensity) > 0.01f) // Обновляем только при значительном изменении
                {
                    interferenceComp.Multiplier = intensity;
                    Dirty(playerUid, interferenceComp);
                }
            }
        }
    }

    /// <summary>
    /// Настраивает физическую зону для ЭМИ эффекта.
    /// </summary>
    private void SetupEmpZone(EntityUid uid, EmpZoneComponent component)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
        {
            Logger.Warning($"EmpZone {uid} has no PhysicsComponent");
            return;
        }

        // Создаем круговую фикстуру
        var shape = new PhysShapeCircle(component.Radius);
        var collisionLayer = (int) CollisionGroup.MobLayer;
        var collisionMask = (int) CollisionGroup.MobLayer;
        var isSensor = true; // Важно! Делаем сенсором, чтобы не блокировать движение

        var success = _fixtures.TryCreateFixture(uid, shape, component.FixtureId, hard: !isSensor, collisionLayer: collisionLayer, collisionMask: collisionMask);

        if (success)
        {
            Logger.Info($"EmpZone fixture created successfully for {uid}");
        }
        else
        {
            Logger.Error($"Failed to create EmpZone fixture for {uid}");
        }
    }

    /// <summary>
    /// Применяет ЭМИ эффект с интенсивностью, зависящей от расстояния.
    /// </summary>
        private void ApplyEmpEffect(EntityUid player, EntityUid empZone, EmpZoneComponent component)
    {
        Logger.Info($"Starting ApplyEmpEffect for player {player}");

        if (!TryComp<StatusEffectsComponent>(player, out var statusComp))
        {
            Logger.Warning($"Player {player} has no StatusEffectsComponent");
            return;
        }

        Logger.Info($"Player {player} has StatusEffectsComponent");

        // Вычисляем расстояние между игроком и центром зоны
        var playerPos = _transform.GetWorldPosition(player);
        var zonePos = _transform.GetWorldPosition(empZone);
        var distance = Vector2.Distance(playerPos, zonePos);

        // Вычисляем интенсивность на основе расстояния
        var intensity = CalculateIntensity(distance, component);

        Logger.Info($"Applying EMP effect to {player}, distance: {distance:F2}, intensity: {intensity:F2}");

                // Применяем эффект с бесконечной длительностью (пока игрок в зоне)
        var infiniteDuration = TimeSpan.FromDays(365); // Практически бесконечно
        Logger.Info($"Duration: Infinite (until player leaves zone)");

        try
        {
            // Проверим, есть ли уже эффект
            if (_status.HasStatusEffect(player, "EmpInterference"))
            {
                Logger.Info($"Player {player} already has EmpInterference effect, refreshing");
                // Если эффект уже есть, просто обновляем интенсивность
                if (TryComp<EmpInterferenceComponent>(player, out var existingComp))
                {
                    existingComp.Multiplier = intensity;
                    Dirty(player, existingComp);
                    Logger.Info($"Updated existing effect intensity to {intensity:F2} for {player}");
                    return;
                }
            }

            Logger.Info($"Attempting to add EmpInterference effect to {player}");

            // Попробуем сначала применить простой эффект для теста
            var testSuccess = _status.TryAddStatusEffect<SeeingStaticComponent>(player, "SeeingStatic", TimeSpan.FromSeconds(5), false, statusComp);
            Logger.Info($"Test effect (SeeingStatic) result: {testSuccess}");

            // Теперь пробуем наш эффект
            var success = _status.TryAddStatusEffect<EmpInterferenceComponent>(player, "EmpInterference", infiniteDuration, false, statusComp);
            Logger.Info($"TryAddStatusEffect result: {success}");

            if (success)
            {
                Logger.Info($"EMP effect applied successfully to {player}");

                // Устанавливаем начальный множитель интенсивности
                if (TryComp<EmpInterferenceComponent>(player, out var interferenceComp))
                {
                    interferenceComp.Multiplier = intensity;
                    interferenceComp.Duration = float.MaxValue; // Бесконечная длительность
                    Dirty(player, interferenceComp);
                    Logger.Info($"Set initial intensity multiplier to {intensity:F2} for {player}");
                }
                else
                {
                    Logger.Warning($"Player {player} has no EmpInterferenceComponent after applying effect");
                }
            }
            else
            {
                Logger.Error($"Failed to apply EMP effect to {player}");

                // Попробуем диагностировать проблему
                Logger.Info($"Player {player} StatusEffectsComponent: {statusComp != null}");
                Logger.Info($"Duration: {infiniteDuration}");
                Logger.Info($"Effect key: EmpInterference");

                // Проверим, есть ли конфликтующие эффекты
                Logger.Info($"Player {player} StatusEffectsComponent valid: {statusComp != null}");
                Logger.Info($"Effect key being used: EmpInterference");
                Logger.Info($"Duration being set: {infiniteDuration}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Exception while applying EMP effect to {player}: {ex}");
        }
    }

    /// <summary>
    /// Убирает ЭМИ эффект с игрока.
    /// </summary>
    private void RemoveEmpEffect(EntityUid player)
    {
        if (!TryComp<StatusEffectsComponent>(player, out var statusComp))
            return;

        _status.TryRemoveStatusEffect(player, "EmpInterference", statusComp);
    }

    /// <summary>
    /// Обновляет интенсивность ЭМИ эффекта для игрока.
    /// </summary>
    private void UpdateEmpEffectIntensity(EntityUid player, float intensity)
    {
        if (TryComp<EmpInterferenceComponent>(player, out var interferenceComp))
        {
            interferenceComp.Multiplier = intensity;
            Dirty(player, interferenceComp);
        }
    }

    /// <summary>
    /// Вычисляет интенсивность эффекта на основе расстояния до центра зоны.
    /// </summary>
    private float CalculateIntensity(float distance, EmpZoneComponent component)
    {
        // Если игрок в центре зоны (расстояние = 0), интенсивность максимальная
        if (distance <= 0)
            return component.MaxIntensity;

        // Если игрок за пределами зоны, интенсивность минимальная
        if (distance >= component.Radius)
            return component.MinIntensity;

        // Линейная интерполяция между максимальной и минимальной интенсивностью
        var normalizedDistance = distance / component.Radius;
        var intensity = component.MaxIntensity - (component.MaxIntensity - component.MinIntensity) * normalizedDistance;

        return Math.Clamp(intensity, component.MinIntensity, component.MaxIntensity);
    }

    /// <summary>
    /// Включает или выключает зону ЭМИ.
    /// </summary>
    public void SetEmpZoneEnabled(EntityUid uid, bool enabled, EmpZoneComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Enabled = enabled;

        if (enabled)
        {
            SetupEmpZone(uid, component);
        }
        else
        {
            // Убираем эффект со всех игроков
            var query = EntityQueryEnumerator<StatusEffectsComponent>();
            while (query.MoveNext(out var playerUid, out var statusComp))
            {
                if (HasComp<MindContainerComponent>(playerUid))
                {
                    _status.TryRemoveStatusEffect(playerUid, "SeeingStatic", statusComp);
                }
            }

            // Удаляем фикстуру
            _fixtures.DestroyFixture(uid, component.FixtureId);
        }
    }
}
