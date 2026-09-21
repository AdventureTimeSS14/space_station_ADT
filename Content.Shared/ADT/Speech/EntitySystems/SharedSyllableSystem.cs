// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Speech.EntitySystems;

public abstract class SharedSyllableSystem : EntitySystem
{
    public static readonly ProtoId<StatusEffectPrototype> SyllableKey = "Syllable";

    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    // For code in shared... I imagine we ain't getting accent prediction anytime soon so let's not bother.
    public virtual void DoSyllable(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
    }

    public void DoRemoveSyllable(EntityUid uid)
    {
        _statusEffectsSystem.TryRemoveStatusEffect(uid, SyllableKey);
    }
}
