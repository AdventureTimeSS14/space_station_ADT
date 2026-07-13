// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.ADT.Blob.Components;

namespace Content.Server.ADT.Blob.Objectives;

[RegisterComponent]
public sealed partial class BlobCaptureConditionComponent : Component
{
    [DataField]
    public int Target { get; set; } = StationBlobConfigComponent.DefaultStageEnd;
}