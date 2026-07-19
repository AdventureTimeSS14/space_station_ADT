// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Changeling.Components;
using Content.Shared.Body; // ADT-Tweak
using Content.Shared.Eye.Blinding.Components;

namespace Content.Goobstation.Shared.Changeling.Systems;

public abstract class SharedChangelingSystem : EntitySystem
{
    [Dependency] protected readonly BodySystem Body = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected virtual void UpdateFlashImmunity(EntityUid uid, bool active) { }
}
