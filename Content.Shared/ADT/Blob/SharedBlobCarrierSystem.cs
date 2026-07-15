// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Blob;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Blob;

public abstract class SharedBlobCarrierSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public static readonly TimeSpan MinRoundDurationToTransform = TimeSpan.FromMinutes(25);

    public bool CanTransform()
    {
        return _gameTicker.RoundDuration() >= MinRoundDurationToTransform;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var blobFactoryQuery = EntityQueryEnumerator<BlobCarrierComponent>();
        while (blobFactoryQuery.MoveNext(out var ent, out var comp))
        {
            if (!comp.HasMind)
                continue;

            comp.TransformationTimer += frameTime;

            if (_gameTiming.CurTime < comp.NextAlert)
                continue;

            var remainingTime = Math.Round(comp.TransformationDelay - comp.TransformationTimer, 0);
            if (remainingTime > 0)
                _popup.PopupClient(Loc.GetString("carrier-blob-alert", ("second", remainingTime)), ent, ent, PopupType.LargeCaution);
            else if (!CanTransform())
                _popup.PopupClient(Loc.GetString("carrier-blob-too-early"), ent, ent, PopupType.LargeCaution);

            comp.NextAlert = _gameTiming.CurTime + TimeSpan.FromSeconds(comp.AlertInterval);

            if (remainingTime > 0 || !CanTransform())
                continue;

            TransformToBlob((ent, comp));
        }
    }

    protected abstract void TransformToBlob(Entity<BlobCarrierComponent> ent);
}