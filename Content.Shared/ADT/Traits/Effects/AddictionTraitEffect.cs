// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.ADT.Addiction;

namespace Content.Shared.ADT.Traits.Effects;

/// <summary>
/// Эффект трайта зависимости: добавляет канал в существующий AddictionComponent
/// (компонент у всех игроков один, AddCompsEffect его не мержит - при нескольких
/// трайтах добавлялся бы только первый канал).
/// </summary>
public sealed partial class AddictionTraitEffect : BaseTraitEffect
{
    /// <summary>
    /// Канал зависимости (алкоголь/никотин/наркотики).
    /// </summary>
    [DataField(required: true)]
    public AddictionKind Kind;

    /// <summary>
    /// Стартовый уровень привыкания (100 = тяжёлая стадия).
    /// </summary>
    [DataField]
    public float Level = 100f;

    /// <summary>
    /// Трайтовая зависимость неизлечима: уровень не спадает, детоксин не берёт.
    /// </summary>
    [DataField]
    public bool Permanent = true;

    public override void Apply(TraitEffectContext ctx)
    {
        var comp = ctx.EntMan.EnsureComponent<AddictionComponent>(ctx.Player);

        // Защита от дублей (один и тот же трайт дважды)
        if (comp.Channels.Any(c => c.Kind == Kind))
            return;

        comp.Channels.Add(new AddictionChannel
        {
            Kind = Kind,
            Level = Level,
            Permanent = Permanent,
        });
    }
}
