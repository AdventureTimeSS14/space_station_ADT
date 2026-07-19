// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.ADT.Blob.Objectives;

[RegisterComponent]
public sealed partial class BlobCaptureConditionComponent : Component
{
    [DataField]
    public int Target { get; set; } = 800;
}