// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Popups;
using Content.Shared.ADT.Addiction;
using Content.Shared.ADT.Addiction.Effects;
using Content.Shared.EntityEffects;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Addiction;

/// <summary>
/// Применяет эффект AdjustAddictionLevel: отрицательный Amount лечит (Детоксин),
/// положительный Amount качает канал (передоз лекарств, канал создаётся при необходимости).
/// Трайтовые зависимости (Permanent) не лечатся.
/// </summary>
public sealed partial class AdjustAddictionLevelEffectSystem : EntityEffectSystem<AddictionComponent, AdjustAddictionLevel>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    protected override void Effect(Entity<AddictionComponent> entity, ref EntityEffectEvent<AdjustAddictionLevel> args)
    {
        var amount = args.Effect.Amount * args.Scale;

        if (amount > 0)
        {
            // Передоз: качаем конкретный канал (Kind обязателен), создаём при отсутствии.
            if (args.Effect.Kind is not { } kind)
                return;

            var channel = entity.Comp.Channels.FirstOrDefault(c => c.Kind == kind);
            if (channel == null)
            {
                channel = new AddictionChannel { Kind = kind, LastDoseTime = _timing.CurTime };
                entity.Comp.Channels.Add(channel);
            }

            channel.Level = MathF.Min(100f, channel.Level + amount);
            channel.LastDoseTime = _timing.CurTime;
            channel.NextPopupTime = TimeSpan.Zero;

            // Доза снимает ломку (как у остальных каналов в AddictionSystem)
            if (channel.InWithdrawal)
            {
                channel.InWithdrawal = false;
                channel.Stage = 0;
                var symptomsEv = new AddictionSymptomsChangedEvent(entity.Owner);
                RaiseLocalEvent(entity.Owner, ref symptomsEv);
                _popup.PopupEntity(Loc.GetString($"addiction-dose-{KindLoc(kind)}"), entity.Owner, entity.Owner);
            }
            // Первое превышение порога - подсадка
            else if (!channel.WasAddicted && channel.Level >= entity.Comp.Threshold)
            {
                channel.WasAddicted = true;
                _popup.PopupEntity(Loc.GetString($"addiction-begin-{KindLoc(kind)}"), entity.Owner, entity.Owner);
            }
            return;
        }

        // Лечение: снижаем уровень, трайтовые (Permanent) не трогаем.
        foreach (var channel in entity.Comp.Channels)
        {
            if (channel.Permanent)
                continue;

            if (args.Effect.Kind is { } kind && channel.Kind != kind)
                continue;

            channel.Level = MathF.Max(0f, channel.Level + amount);
        }
    }

    private static string KindLoc(AddictionKind kind) => kind switch
    {
        AddictionKind.Alcohol => "alcohol",
        AddictionKind.Nicotine => "nicotine",
        AddictionKind.Drug => "drug",
        AddictionKind.Medicine => "medicine",
        _ => "alcohol",
    };
}
