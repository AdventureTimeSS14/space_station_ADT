// SPDX-License-Identifier: AGPL-3.0-or-later

//using Content.Server.SpecForces;
namespace Content.Server.ADT.Blob.Components;

[RegisterComponent]
public sealed partial class StationBlobConfigComponent : Component
{
    public const int DefaultStageBegin = 60;
    public const int DefaultStageCritical = 800;
    public const float DefaultStageTheEndPercent = 0.70f;

    [DataField]
    public int StageBegin { get; set; } = DefaultStageBegin;

    [DataField]
    public int StageCritical { get; set; } = DefaultStageCritical;

    [DataField]
    public float StageTheEndPercent { get; set; } = DefaultStageTheEndPercent;

    /*[DataField("specForceTeam")]  //Goobstation - Disabled automatic ERT
    public ProtoId<SpecForceTeamPrototype> SpecForceTeam { get; set; } = "RXBZZBlobDefault";*/
}
