// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Blob.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Blob.Components;

[RegisterComponent]
public sealed partial class BlobFactoryComponent : Component
{
    [DataField("spawnLimit"), ViewVariables(VVAccess.ReadWrite)]
    public float SpawnLimit = 3;

    [DataField("blobSporeId"), ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId<BlobMobComponent> Pod = "ADTMobBlobPod";

    [DataField("blobbernautId"), ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId<BlobbernautComponent> BlobbernautId = "ADTMobBlobBlobbernaut";

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Blobbernaut = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> BlobPods = new ();

    [DataField]
    public int Accumulator = 0;

    [DataField]
    public int AccumulateToSpawn = 3;
}

public sealed class ProduceBlobbernautEvent : EntityEventArgs
{
}
