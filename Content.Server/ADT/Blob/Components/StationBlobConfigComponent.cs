// SPDX-License-Identifier: AGPL-3.0-or-later

//using Content.Server.SpecForces;
namespace Content.Server.ADT.Blob.Components;

[RegisterComponent]
public sealed partial class StationBlobConfigComponent : Component
{
    public const int DefaultStageBegin = 60;
    public const int DefaultStageCritical = 400;
    public const int DefaultStageEnd = 800;

    [DataField]
    public int StageBegin { get; set; } = DefaultStageBegin;

    [DataField]
    public int StageCritical { get; set; } = DefaultStageCritical;

    [DataField]
    public int StageTheEnd { get; set; } = DefaultStageEnd;

    /*[DataField("specForceTeam")]  //Goobstation - Disabled automatic ERT
    public ProtoId<SpecForceTeamPrototype> SpecForceTeam { get; set; } = "RXBZZBlobDefault";*/
}