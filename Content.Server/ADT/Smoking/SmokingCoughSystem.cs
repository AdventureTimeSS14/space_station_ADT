// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Nutrition.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Smoking;

/// <summary>
/// Кашель при курении: пока игрок курит (горящий smokable во рту), каждые 25 секунд
/// он кашляет через эмоцию Cough (сообщение в чат + звук). Вейпы не кашляют.
/// Условия зеркалят SmokingSystem: smokable в слоте mask живого владельца с кровью,
/// в растворе ещё есть чем затянуться.
/// </summary>
public sealed partial class SmokingCoughSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    /// <summary>
    /// Интервал между кашлями, пока игрок курит.
    /// </summary>
    private static readonly TimeSpan CoughInterval = TimeSpan.FromSeconds(25);

    /// <summary>
    /// Как часто проверяем курящих.
    /// </summary>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Время следующего кашля для каждого курящего игрока. Запись живёт, пока игрок курит;
    /// перестал курить - запись удаляется, следующая сессия курения начинает отсчёт заново.
    /// </summary>
    private readonly Dictionary<EntityUid, TimeSpan> _nextCough = new();

    private TimeSpan _nextCheck;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextCheck)
            return;

        _nextCheck = _timing.CurTime + CheckInterval;

        var seen = new HashSet<EntityUid>();
        var query = EntityQueryEnumerator<BurningComponent, SmokableComponent>();
        while (query.MoveNext(out var uid, out _, out var smokable))
        {
            // Вейпы не кашляют
            if (HasComp<VapeComponent>(uid))
                continue;

            // Смоукл во рту живого владельца с кровью (условия SmokingSystem)
            if (!_container.TryGetContainingContainer((uid, null, null), out var containerManager) ||
                !_inventory.TryGetSlotEntity(containerManager.Owner, "mask", out var inMaskSlotUid) ||
                inMaskSlotUid != uid ||
                !TryComp(containerManager.Owner, out BloodstreamComponent? bloodstream) ||
                !_mobState.IsAlive(containerManager.Owner))
            {
                continue;
            }

            var smoker = containerManager.Owner;
            seen.Add(smoker);

            // В растворе ещё есть чем затянуться (без этого курить нечего)
            if (!_solution.TryGetSolution(uid, smokable.Solution, out _, out var solution) ||
                solution.Volume == FixedPoint2.Zero)
            {
                continue;
            }

            if (!_nextCough.TryGetValue(smoker, out var nextCough))
            {
                _nextCough[smoker] = _timing.CurTime + CoughInterval;
                continue;
            }

            if (_timing.CurTime < nextCough)
                continue;

            _chat.TryEmoteWithChat(smoker, "Cough", hideLog: true);
            _nextCough[smoker] = _timing.CurTime + CoughInterval;
        }

        // Игроки, которые больше не курят: сбрасываем отсчёт,
        // следующая сессия курения начнёт 25 секунд заново
        foreach (var smoker in _nextCough.Keys.ToList())
        {
            if (!seen.Contains(smoker))
                _nextCough.Remove(smoker);
        }
    }
}
