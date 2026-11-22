// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Shared.ADT.Addiction.Symptoms;

public sealed partial class AddictionJitterSymptom : AddictionSymptom
{
    [DataField]
    public float Amplitude = 10f;

    [DataField]
    public float Frequency = 3f;

    /// <summary>
    /// Должна быть больше AddictionComponent.SymptomRefreshInterval.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(35);

    public override string Key => "jitter";

    public override bool Apply(AddictionSymptomContext ctx)
    {
        ctx.EntMan.System<SharedJitteringSystem>()
            .DoJitter(ctx.Target, Duration, refresh: true, Amplitude, Frequency);
        return true;
    }

    public override void Remove(AddictionSymptomContext ctx)
    {
        ctx.EntMan.RemoveComponent<JitteringComponent>(ctx.Target);
    }
}

public sealed partial class AddictionStatusEffectSymptom : AddictionSymptom
{
    [DataField(required: true)]
    public EntProtoId Effect;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(35);

    public override string Key => $"status:{Effect.Id}";

    public override bool Apply(AddictionSymptomContext ctx)
    {
        ctx.EntMan.System<StatusEffectsSystem>()
            .TrySetStatusEffectDuration(ctx.Target, Effect, Duration);
        return true;
    }

    public override void Remove(AddictionSymptomContext ctx)
    {
        ctx.EntMan.System<StatusEffectsSystem>().TryRemoveStatusEffect(ctx.Target, Effect);
    }
}

/// <summary>
/// Компоненты на время ломки.
/// </summary>
public sealed partial class AddictionComponentSymptom : AddictionSymptom
{
    [DataField(required: true, customTypeSerializer: typeof(ComponentRegistrySerializer))]
    public ComponentRegistry Components = new();

    private string? _key;

    public override string Key => _key ??= $"comps:{string.Join(',', Components.Keys.OrderBy(key => key))}";

    public override bool Apply(AddictionSymptomContext ctx)
    {
        // Если что-то из набора уже стоит, значит его поставили не мы.
        if (!ctx.Owned)
        {
            foreach (var (_, entry) in Components)
            {
                if (ctx.EntMan.HasComponent(ctx.Target, entry.Component.GetType()))
                    return false;
            }

            ctx.EntMan.AddComponents(ctx.Target, Components, removeExisting: false);
        }

        return true;
    }

    public override void Remove(AddictionSymptomContext ctx)
    {
        ctx.EntMan.RemoveComponents(ctx.Target, Components);
    }
}
