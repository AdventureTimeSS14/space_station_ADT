// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Addiction;
using Content.Shared.ADT.Addiction.Effects;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Addiction;

/// <summary>
/// Permanent-каналы этим эффектом не лечатся.
/// </summary>
public sealed partial class AdjustAddictionLevelEffectSystem : EntityEffectSystem<AddictionComponent, AdjustAddictionLevel>
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private AddictionSystem _addiction = default!;

    protected override void Effect(Entity<AddictionComponent> entity, ref EntityEffectEvent<AdjustAddictionLevel> args)
    {
        var amount = args.Effect.Amount * args.Scale;

        if (amount > 0)
        {
            if (args.Effect.Kind is not { } kind)
                return;

            if (!_proto.TryIndex(kind, out var proto))
                return;

            _addiction.ApplyDose(entity.Owner, _addiction.EnsureChannel(entity.Comp, kind), proto, amount);
            return;
        }

        foreach (var channel in entity.Comp.Channels)
        {
            if (channel.Permanent)
                continue;

            if (args.Effect.Kind is { } target && channel.Kind != target)
                continue;

            channel.Level = MathF.Max(0f, channel.Level + amount);
        }
    }
}
