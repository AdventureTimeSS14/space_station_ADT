// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Addiction;

namespace Content.Shared.ADT.Traits.Effects;

/// <summary>
/// Эффект трайта «Случайная зависимость»: помечает компонент, чтобы AddictionSystem
/// при спавне (после TraitSystem) выбрала случайный канал из ещё не выбранных.
/// Канал получается неизлечимым (Permanent), как и у конкретных трайтов.
/// </summary>
public sealed partial class RandomAddictionEffect : BaseTraitEffect
{
    public override void Apply(TraitEffectContext ctx)
    {
        var comp = ctx.EntMan.EnsureComponent<AddictionComponent>(ctx.Player);
        comp.RandomizeChannel = true;
    }
}
