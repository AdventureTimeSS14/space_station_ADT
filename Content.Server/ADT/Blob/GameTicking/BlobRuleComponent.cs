// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking.Rules;
using Content.Shared.Mind;
using Robust.Shared.Audio;

namespace Content.Server.ADT.Blob.GameTicking;

[RegisterComponent, Access(typeof(BlobRuleSystem), typeof(BlobCoreSystem), typeof(BlobObserverSystem))]
public sealed partial class BlobRuleComponent : Component
{
    [DataField]
    public SoundSpecifier? DetectedAudio = null;

    [DataField]
    public SoundSpecifier? CriticalAudio = null;

    [ViewVariables]
    public List<(EntityUid mindId, MindComponent mind)> Blobs = new(); //BlobRoleComponent

    [ViewVariables]
    public BlobStage Stage = BlobStage.Default;

    [ViewVariables]
    public float Accumulator = 0f;

    [ViewVariables]
    public bool ShuttleArrivedAnnounced = false;
}


public enum BlobStage : byte
{
    Default,
    Begin,
    Critical,
    TheEnd,
}