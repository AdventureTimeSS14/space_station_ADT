// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Addiction;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Traits.Effects;

/// <summary>
/// Эффект трайта «Случайная зависимость»: кладёт в компонент заготовку канала и пул видов,
/// а AddictionSystem при спавне (после ADTTraitSystem) выбирает вид из ещё не занятых.
/// Выбирать нужно именно там: к моменту применения эффекта каналы других трайтов
/// могут быть ещё не добавлены.
/// </summary>
public sealed partial class RandomAddictionEffect : BaseTraitEffect
{
    /// <summary>
    /// Из каких зависимостей выбирать.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<AddictionPrototype>> Pool = new();

    /// <summary>
    /// Стартовый уровень привыкания.
    /// </summary>
    [DataField]
    public float Level = 100f;

    /// <summary>
    /// Случайная зависимость так же неизлечима, как и конкретные трайты.
    /// </summary>
    [DataField]
    public bool Permanent = true;

    public override void Apply(TraitEffectContext ctx)
    {
        var comp = ctx.EntMan.EnsureComponent<AddictionComponent>(ctx.Player);

        comp.PendingRandomPool = Pool;
        comp.PendingRandomChannel = new AddictionChannel
        {
            Level = Level,
            Permanent = Permanent,
        };
    }
}
