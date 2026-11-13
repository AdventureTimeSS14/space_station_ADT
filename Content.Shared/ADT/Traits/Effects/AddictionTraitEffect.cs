// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.ADT.Addiction;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Traits.Effects;

/// <summary>
/// Добавляет канал в существующий AddictionComponent: AddCompsEffect компонент не мержит.
/// </summary>
public sealed partial class AddictionTraitEffect : BaseTraitEffect
{
    [DataField("kind", required: true)]
    public ProtoId<AddictionPrototype> Kind;

    [DataField]
    public float Level = 100f;

    [DataField]
    public bool Permanent = true;

    public override void Apply(TraitEffectContext ctx)
    {
        var comp = ctx.EntMan.EnsureComponent<AddictionComponent>(ctx.Player);

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
