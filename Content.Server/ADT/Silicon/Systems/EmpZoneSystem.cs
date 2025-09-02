using Content.Shared.ADT.Silicon.Components;
using Content.Shared.ADT.Silicon.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.StatusEffect;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Server.ADT.Silicon.Systems;

/// <summary>
/// Система для обработки зон ЭМИ эффекта с переменной интенсивностью.
/// </summary>
public sealed class EmpZoneSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    // Буферы для минимизации аллокаций каждый кадр
    private readonly Dictionary<EntityUid, (float DurSec, float Mult)> _pending = new();
    // Отслеживаем, кому мы добавили SeeingStatic в allowed
    private readonly HashSet<EntityUid> _allowedSeeingStatic = new();

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _pending.Clear();

        // Обрабатываем активные зоны через пространственный поиск
        var zoneQuery = EntityQueryEnumerator<EmpZoneComponent, TransformComponent>();
        while (zoneQuery.MoveNext(out var zoneUid, out var zone, out var xform))
        {
            if (!zone.Enabled)
                continue;

            // Координаты карты для EntityLookupSystem
            var worldPos = _transform.GetWorldPosition(xform);
            var mapCoords = new MapCoordinates(worldPos, xform.MapID);

            foreach (var uid in _lookup.GetEntitiesInRange(mapCoords, zone.Radius))
            {
                // Не накладываем эффект на сам источник зоны
                if (HasComp<EmpZoneComponent>(uid))
                    continue;

                if (!HasComp<MindContainerComponent>(uid))
                    continue;

                // Проверяем наличие контейнера статусов
                if (!TryComp<StatusEffectsComponent>(uid, out var status))
                    continue;

                // Добавляем SeeingStatic в allowed, если его там нет
                if (!status.AllowedEffects.Contains(SharedSeeingStaticSystem.StaticKey))
                {
                    status.AllowedEffects.Add(SharedSeeingStaticSystem.StaticKey);
                    _allowedSeeingStatic.Add(uid);
                    Dirty(uid, status);
                }

                // Расстояние до центра для вычисления интенсивности (меньше расстояние → выше мультипликатор)
                if (!TryComp<TransformComponent>(uid, out var targetXform))
                    continue;

                var zonePos = _transform.GetWorldPosition(xform);
                var targetPos = _transform.GetWorldPosition(targetXform);
                var distance = Vector2.Distance(targetPos, zonePos);
                var norm = Math.Clamp(distance / Math.Max(0.01f, zone.Radius), 0f, 1f);
                var mult = zone.MaxIntensity - (zone.MaxIntensity - zone.MinIntensity) * norm;

                // Копим максимум длительности и максимум мультипликатора при пересечении нескольких зон
                var dur = zone.Duration;
                if (_pending.TryGetValue(uid, out var cur))
                {
                    var bestDur = dur > cur.DurSec ? dur : cur.DurSec;
                    var bestMult = mult > cur.Mult ? mult : cur.Mult;
                    _pending[uid] = (bestDur, bestMult);
                }
                else
                {
                    _pending[uid] = (dur, mult);
                }
            }
        }

        if (_pending.Count == 0)
        {
            // Никто не в зоне: снимаем эффекты только у тех, у кого они активны
            var activeStaticQuery = EntityQueryEnumerator<StatusEffectsComponent, SeeingStaticComponent>();
            while (activeStaticQuery.MoveNext(out var uid, out var status, out var _))
            {
                _status.TryRemoveStatusEffect(uid, SharedSeeingStaticSystem.StaticKey, status);
            }
            // Убираем EmpInterference компоненты у всех
            var activeEmpQuery = EntityQueryEnumerator<EmpInterferenceComponent>();
            while (activeEmpQuery.MoveNext(out var uid, out var _))
            {
                RemComp<EmpInterferenceComponent>(uid);
            }

            // Возвращаем SeeingStatic из allowed тем, кому мы его добавляли
            foreach (var uid in _allowedSeeingStatic)
            {
                if (TryComp<StatusEffectsComponent>(uid, out var status))
                {
                    status.AllowedEffects.Remove(SharedSeeingStaticSystem.StaticKey);
                    Dirty(uid, status);
                }
            }
            _allowedSeeingStatic.Clear();
            return;
        }

        // Применяем/обновляем эффекты тем, кто в зонах
        foreach (var (uid, data) in _pending)
        {
            if (!TryComp<StatusEffectsComponent>(uid, out var status))
                continue;

            // SeeingStatic (визуальный шейдер)
            _status.TryAddStatusEffect<SeeingStaticComponent>(uid, SharedSeeingStaticSystem.StaticKey, TimeSpan.FromSeconds(data.DurSec), true, status);
            var staticComp = EnsureComp<SeeingStaticComponent>(uid);
            if (Math.Abs(staticComp.Multiplier - data.Mult) > 0.01f)
            {
                staticComp.Multiplier = data.Mult;
                Dirty(uid, staticComp);
            }

            // EmpInterference (геймплейный эффект) - добавляем компонент напрямую
            var empComp = EnsureComp<EmpInterferenceComponent>(uid);
            empComp.Multiplier = data.Mult;
            empComp.Duration = data.DurSec;
            Dirty(uid, empComp);
        }

        // Снимаем эффекты с тех, кто сейчас не в зонах, но эффекты ещё висят
        var removeStaticQuery = EntityQueryEnumerator<StatusEffectsComponent, SeeingStaticComponent>();
        while (removeStaticQuery.MoveNext(out var uid, out var statusComp, out var _))
        {
            if (_pending.ContainsKey(uid))
                continue;

            _status.TryRemoveStatusEffect(uid, SharedSeeingStaticSystem.StaticKey, statusComp);
        }

        // Убираем EmpInterference компоненты у тех, кто не в зонах
        var removeEmpQuery = EntityQueryEnumerator<EmpInterferenceComponent>();
        while (removeEmpQuery.MoveNext(out var uid, out var _))
        {
            if (_pending.ContainsKey(uid))
                continue;

            RemComp<EmpInterferenceComponent>(uid);
        }

        // Возвращаем SeeingStatic из allowed тем, кто вышел из зон
        var toRemove = new List<EntityUid>();
        foreach (var uid in _allowedSeeingStatic)
        {
            if (!_pending.ContainsKey(uid))
            {
                if (TryComp<StatusEffectsComponent>(uid, out var status))
                {
                    status.AllowedEffects.Remove(SharedSeeingStaticSystem.StaticKey);
                    Dirty(uid, status);
                }
                toRemove.Add(uid);
            }
        }
        foreach (var uid in toRemove)
        {
            _allowedSeeingStatic.Remove(uid);
        }
    }
}
