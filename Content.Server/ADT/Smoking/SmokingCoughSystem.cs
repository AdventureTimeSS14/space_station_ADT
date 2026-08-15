// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

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
/// Кашель каждые 25 секунд, пока игрок курит.
/// </summary>
public sealed partial class SmokingCoughSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private static readonly TimeSpan CoughInterval = TimeSpan.FromSeconds(25);

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

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
            if (HasComp<VapeComponent>(uid))
                continue;

            if (!_container.TryGetContainingContainer((uid, null, null), out var containerManager) ||
                !_inventory.TryGetSlotEntity(containerManager.Owner, "mask", out var inMaskSlotUid) ||
                inMaskSlotUid != uid ||
                !TryComp(containerManager.Owner, out BloodstreamComponent? bloodstream) ||
                !_mobState.IsAlive(containerManager.Owner))
            {
                continue;
            }

            if (!_solution.TryGetSolution(uid, smokable.Solution, out _, out var solution) ||
                solution.Volume == FixedPoint2.Zero)
            {
                continue;
            }

            var smoker = containerManager.Owner;
            seen.Add(smoker);

            var cough = EnsureComp<ADTSmokingCoughComponent>(smoker);
            if (cough.NextCough == default)
                cough.NextCough = _timing.CurTime + CoughInterval;

            if (_timing.CurTime < cough.NextCough)
                continue;

            _chat.TryEmoteWithChat(smoker, "Cough", hideLog: true);
            cough.NextCough = _timing.CurTime + CoughInterval;
        }

        var coughQuery = EntityQueryEnumerator<ADTSmokingCoughComponent>();
        while (coughQuery.MoveNext(out var uid, out _))
        {
            if (!seen.Contains(uid))
                RemCompDeferred<ADTSmokingCoughComponent>(uid);
        }
    }
}
