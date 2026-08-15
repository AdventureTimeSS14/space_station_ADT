// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.RPD.Components;
using Content.Shared.ADT.RPD.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.ADT.RPD;

/// <summary>Alt+клик по тайлу ставит вторичную конфигурацию РРТ, ваниль не меняем.</summary>
public sealed class RPDAltUseSystem : EntitySystem
{
    [Dependency] private readonly RPDSystem _rpd = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedPlayerRateLimitManager _rateLimit = default!;

    public override void Initialize()
    {
        CommandBinds.Builder
            .BindBefore(ContentKeyFunctions.AltActivateItemInWorld,
                new PointerInputCmdHandler(HandleAltUse), typeof(SharedInteractionSystem))
            .Register<RPDAltUseSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<RPDAltUseSystem>();
    }

    private bool HandleAltUse(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        // Только клики по тайлу, Alt по сущностям остаётся ванильным
        if (uid.Valid)
            return false;

        if (session?.AttachedEntity is not { } user)
            return false;

        if (!coords.IsValid(EntityManager))
            return false;

        if (_rateLimit.CountAction(session!, SharedInteractionSystem.RateLimitKey) != RateLimitStatus.Allowed)
            return false;

        if (!_hands.TryGetActiveItem(user, out var held) || !TryComp<RPDComponent>(held, out var rpd))
            return false;

        if (!_actionBlocker.CanInteract(user, null))
            return false;

        return _rpd.TryStartRPDOperation(held.Value, rpd, user, coords, null, true);
    }
}
