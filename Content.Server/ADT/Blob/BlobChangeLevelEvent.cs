// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.ADT.Blob.GameTicking;
using Content.Server.GameTicking.Rules.Components;

namespace Content.Server.ADT.Blob;

public sealed class BlobChangeLevelEvent : EntityEventArgs
{
    public EntityUid Station;
    public BlobStage Level;
}