// SPDX-License-Identifier: AGPL-3.0-or-later

//using Content.Server.SpecForces;
namespace Content.Server.ADT.Blob.Components;

[RegisterComponent]
public sealed partial class StationBlobConfigComponent : Component
{
    public const int DefaultStageBegin = 60;
    public const float DefaultStageCriticalPercent = 0.5f;
    public const float DefaultStageTheEndPercent = 0.8f;

    [DataField]
    public int StageBegin { get; set; } = DefaultStageBegin;

    [DataField]
    public float StageCriticalPercent { get; set; } = DefaultStageCriticalPercent;

    [DataField]
    public float StageTheEndPercent { get; set; } = DefaultStageTheEndPercent;

    /*[DataField("specForceTeam")]  //Goobstation - Disabled automatic ERT
    public ProtoId<SpecForceTeamPrototype> SpecForceTeam { get; set; } = "RXBZZBlobDefault";*/
}