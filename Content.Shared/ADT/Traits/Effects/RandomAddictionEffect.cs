// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Addiction;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Traits.Effects;

/// <summary>
/// Кладёт в компонент заготовку канала и пул видов, вид выбирает AddictionSystem при спавне.
/// </summary>
public sealed partial class RandomAddictionEffect : BaseTraitEffect
{
    [DataField(required: true)]
    public List<ProtoId<AddictionPrototype>> Pool = new();

    [DataField]
    public float Level = 100f;

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
