// SPDX-FileCopyrightText: 2026 Kirill Golubenko
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT
//
// Author: Kirill Golubenko
// Discord: golub4ik
// Ckey: WikiHampter

using System.Text;
using Content.Server.ADT.BloodBrothers.Components;
using Content.Server.ADT.BloodBrothers.Objectives.Components;
using Content.Server.Antag;
using Robust.Shared.Localization;
using Content.Server.GameTicking.Rules;
using Content.Server.Objectives;
using Content.Server.Mind;
using Content.Shared.ADT.BloodBrothers.Components;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Server.GameObjects;

namespace Content.Server.ADT.BloodBrothers;

public sealed partial class BloodBrotherRuleSystem : GameRuleSystem<BloodBrotherRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
        SubscribeLocalEvent<BloodBrotherRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
        SubscribeLocalEvent<BloodBrotherDocumentsComponent, MapInitEvent>(OnFolderMapInit);
    }

    private void OnAntagSelect(Entity<BloodBrotherRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mindSystem.TryGetMind(args.EntityUid, out var mindId, out var mind))
            return;

        ent.Comp.PendingMinds.Add(mindId);

        if (ent.Comp.PendingMinds.Count >= 2)
        {
            var team = new BloodBrotherTeam
            {
                MemberMinds = new List<EntityUid>(),
                MeetingArea = _random.Pick(ent.Comp.MeetingAreas),
            };

            for (var i = 0; i < 2; i++)
            {
                team.MemberMinds.Add(ent.Comp.PendingMinds[0]);
                ent.Comp.PendingMinds.RemoveAt(0);
            }

            foreach (var memberMind in team.MemberMinds)
            {
                if (TryComp<MindComponent>(memberMind, out var memberMindComp) && memberMindComp.OwnedEntity != null)
                {
                    EnsureComp<BloodBrotherComponent>(memberMindComp.OwnedEntity.Value);

                    _npcFaction.RemoveFaction(memberMindComp.OwnedEntity.Value, ent.Comp.NanoTrasenFaction, false);
                    _npcFaction.AddFaction(memberMindComp.OwnedEntity.Value, ent.Comp.SyndicateFaction);
                }
            }

            AssignTeamObjectives(team, ent);

            var teamId = ent.Comp.Teams.Count;
            foreach (var memberMind in team.MemberMinds)
            {
                if (TryComp<MindComponent>(memberMind, out var memberMindComp) && memberMindComp.OwnedEntity != null)
                {
                    var bb = Comp<BloodBrotherComponent>(memberMindComp.OwnedEntity.Value);
                    bb.TeamId = teamId;
                }
            }

            foreach (var memberMind in team.MemberMinds)
            {
                SendBloodBrotherBriefing(memberMind, team, ent);
            }

            ent.Comp.Teams.Add(team);
            TryAssignDocumentObjectives(ent);
        }
    }

    private void AssignTeamObjectives(BloodBrotherTeam team, Entity<BloodBrotherRuleComponent> ent)
    {
        var objectivesCount = 2;

        for (var i = 0; i < objectivesCount; i++)
        {
            var objectiveId = _random.Pick(ent.Comp.AvailableObjectives);
            AssignSharedObjective(team, objectiveId);
        }

        AssignSharedObjective(team, "BothBrothersEscapeObjective");
    }

    private void AssignSharedObjective(BloodBrotherTeam team, string protoId)
    {
        if (team.MemberMinds.Count == 0)
            return;

        var firstMindId = team.MemberMinds[0];
        if (!TryComp<MindComponent>(firstMindId, out var firstMind))
            return;

        var objectiveUid = Spawn(protoId);
        if (!TryComp<ObjectiveComponent>(objectiveUid, out var objectiveComp))
        {
            Del(objectiveUid);
            Log.Error($"BloodBrothers: invalid objective prototype {protoId}");
            return;
        }

        var ev = new ObjectiveAssignedEvent(firstMindId, firstMind);
        RaiseLocalEvent(objectiveUid, ref ev);
        if (ev.Cancelled)
        {
            Del(objectiveUid);
            return;
        }

        var afterEv = new ObjectiveAfterAssignEvent(firstMindId, firstMind, objectiveComp, MetaData(objectiveUid));
        RaiseLocalEvent(objectiveUid, ref afterEv);

        foreach (var memberMind in team.MemberMinds)
        {
            if (!TryComp<MindComponent>(memberMind, out var mind))
                continue;

            _mindSystem.AddObjective(memberMind, mind, objectiveUid);
        }

        if (TryComp<BothBrothersEscapeConditionComponent>(objectiveUid, out var escapeComp))
            escapeComp.TeamMinds.AddRange(team.MemberMinds);

        var linkedComp = EnsureComp<BloodBrotherLinkedObjectivesComponent>(objectiveUid);
        linkedComp.TeamMinds.AddRange(team.MemberMinds);
    }

    private void TryAssignDocumentObjectives(Entity<BloodBrotherRuleComponent> ent)
    {
        if (ent.Comp.Teams.Count < 2)
            return;

        var teamsWithoutDocs = new List<int>();
        for (var i = 0; i < ent.Comp.Teams.Count; i++)
        {
            if (!ent.Comp.Teams[i].HasDocumentsObjective)
                teamsWithoutDocs.Add(i);
        }

        if (teamsWithoutDocs.Count < 2)
            return;

        // Only one pair (2 teams) gets document objectives
        if (teamsWithoutDocs.Count >= 2)
        {
            var idxA = teamsWithoutDocs[0];
            var idxB = teamsWithoutDocs[1];
            teamsWithoutDocs.RemoveRange(0, 2);

            var scenario = _random.Next(0, 4);
            switch (scenario)
            {
                case 0:
                    AssignDocumentObjective(ent, idxA, 0);
                    AssignDocumentObjective(ent, idxB, 0);
                    break;
                case 1:
                {
                    if (_random.Next(0, 2) == 0)
                    {
                        AssignDocumentObjective(ent, idxA, 1, spawnFolder: false);
                        AssignDocumentObjective(ent, idxB, 2);
                    }
                    else
                    {
                        AssignDocumentObjective(ent, idxA, 2);
                        AssignDocumentObjective(ent, idxB, 1, spawnFolder: false);
                    }
                    break;
                }
                case 2:
                {
                    if (_random.Next(0, 2) == 0)
                    {
                        AssignDocumentObjective(ent, idxA, 3);
                        AssignDocumentObjective(ent, idxB, 4, spawnFolder: false);
                    }
                    else
                    {
                        AssignDocumentObjective(ent, idxA, 4, spawnFolder: false);
                        AssignDocumentObjective(ent, idxB, 3);
                    }
                    break;
                }
                case 3:
                {
                    if (_random.Next(0, 2) == 0)
                    {
                        AssignDocumentObjective(ent, idxA, 0);
                        AssignDocumentObjective(ent, idxB, 2);
                    }
                    else
                    {
                        AssignDocumentObjective(ent, idxA, 2);
                        AssignDocumentObjective(ent, idxB, 0);
                    }
                    break;
                }
            }
        }
    }

    private void AssignDocumentObjective(Entity<BloodBrotherRuleComponent> ent, int teamIndex, int variant, bool spawnFolder = true)
    {
        var team = ent.Comp.Teams[teamIndex];

        var isRed = teamIndex % 2 == 0;
        var startingFolder = isRed ? "BloodBrotherDocumentsRed" : "BloodBrotherDocumentsBlue";
        var requiredFolder = isRed ? "BloodBrotherDocumentsBlue" : "BloodBrotherDocumentsRed";

        var variantProtoId = variant switch
        {
            0 => "BloodBrotherDocumentsExchangeObjective",
            1 => "BloodBrotherDocumentsCaptureObjective",
            2 => "BloodBrotherDocumentsProtectObjective",
            3 => "BloodBrotherDocumentsGiveObjective",
            4 => "BloodBrotherDocumentsReceiveObjective",
            _ => "BloodBrotherDocumentsExchangeObjective"
        };

        var firstMindId = team.MemberMinds[0];
        if (!TryComp<MindComponent>(firstMindId, out var firstMind))
            return;

        var objectiveUid = Spawn(variantProtoId);
        if (!TryComp<ObjectiveComponent>(objectiveUid, out var objectiveComp))
        {
            Del(objectiveUid);
            return;
        }

        if (TryComp<BloodBrotherDocumentsConditionComponent>(objectiveUid, out var docComp))
        {
            docComp.StartingFolder = startingFolder;
            docComp.RequiredFolder = requiredFolder;
            docComp.Variant = variant;
            docComp.TeamMinds.AddRange(team.MemberMinds);
        }

        var showRequiredColor = variant switch
        {
            0 => true,
            1 => true,
            2 => false,
            3 => false,
            4 => true,
            _ => true
        };
        var colorIsRed = showRequiredColor ? !isRed : isRed;
        var colorKey = colorIsRed ? "blood-brother-documents-color-red" : "blood-brother-documents-color-blue";
        var colorName = Loc.GetString(colorKey);
        var variantSuffix = variant switch
        {
            0 => "exchange",
            1 => "capture",
            2 => "protect",
            3 => "give",
            4 => "receive",
            _ => "exchange"
        };
        var nameKey = $"blood-brother-documents-{variantSuffix}-title";
        var descKey = $"blood-brother-documents-{variantSuffix}-desc";
        _meta.SetEntityName(objectiveUid, Loc.GetString(nameKey, ("color", colorName)));
        _meta.SetEntityDescription(objectiveUid, Loc.GetString(descKey, ("color", colorName)));

        var ev = new ObjectiveAssignedEvent(firstMindId, firstMind);
        RaiseLocalEvent(objectiveUid, ref ev);
        if (ev.Cancelled)
        {
            Del(objectiveUid);
            return;
        }

        var afterEv = new ObjectiveAfterAssignEvent(firstMindId, firstMind, objectiveComp, MetaData(objectiveUid));
        RaiseLocalEvent(objectiveUid, ref afterEv);

        foreach (var memberMind in team.MemberMinds)
        {
            if (!TryComp<MindComponent>(memberMind, out var mind))
                continue;
            _mindSystem.AddObjective(memberMind, mind, objectiveUid);
        }

        var linkedComp = EnsureComp<BloodBrotherLinkedObjectivesComponent>(objectiveUid);
        linkedComp.TeamMinds.AddRange(team.MemberMinds);

        if (spawnFolder)
        {
            var folderOwnerMind = team.MemberMinds[_random.Next(0, team.MemberMinds.Count)];
            if (TryComp<MindComponent>(folderOwnerMind, out var folderMind) && folderMind.OwnedEntity != null)
                _inventorySystem.SpawnItemOnEntity(folderMind.OwnedEntity.Value, startingFolder);
        }

        team.HasDocumentsObjective = true;
        ent.Comp.Teams[teamIndex] = team;
    }

    private void OnFolderMapInit(Entity<BloodBrotherDocumentsComponent> ent, ref MapInitEvent args)
    {
        var protoId = MetaData(ent).EntityPrototype?.ID;
        if (protoId == null)
            return;
        var descKey = protoId == "BloodBrotherDocumentsRed"
            ? "blood-brother-documents-red-desc"
            : "blood-brother-documents-blue-desc";
        _meta.SetEntityDescription(ent, Loc.GetString(descKey));
    }

    private void SendBloodBrotherBriefing(EntityUid mindId, BloodBrotherTeam team, Entity<BloodBrotherRuleComponent> ent)
    {
        if (!TryComp<MindComponent>(mindId, out var mind))
            return;

        var playerEntity = mind.OriginalOwnedEntity != null
            ? GetEntity(mind.OriginalOwnedEntity.Value)
            : mind.OwnedEntity;

        if (playerEntity == null || !Exists(playerEntity.Value))
            return;

        var brotherNames = GetBrotherNames(team, mindId);

        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString("blood-brother-role-greeting"));

        if (!string.IsNullOrEmpty(brotherNames))
        {
            sb.AppendLine(Loc.GetString("blood-brother-brothers-names", ("names", brotherNames)));
            sb.AppendLine(Loc.GetString("blood-brother-meeting-area", ("area", Loc.GetString(team.MeetingArea))));
        }

        sb.AppendLine(Loc.GetString("blood-brother-syndicate-end"));

        _antag.SendBriefing(playerEntity.Value, sb.ToString(), Color.FromHex("#cc3b3b"), ent.Comp.GreetSoundNotification);
    }

    private string GetBrotherNames(BloodBrotherTeam team, EntityUid excludeMind)
    {
        var names = new List<string>();
        foreach (var memberMind in team.MemberMinds)
        {
            if (memberMind == excludeMind)
                continue;

            if (TryComp<MindComponent>(memberMind, out var mindComp))
                names.Add(mindComp.CharacterName ?? "Unknown");
        }

        return string.Join(", ", names);
    }

    private void OnObjectivesTextGetInfo(Entity<BloodBrotherRuleComponent> ent, ref ObjectivesTextGetInfoEvent args)
    {
        var result = new List<(EntityUid, string)>();

        foreach (var team in ent.Comp.Teams)
        {
            var names = new List<string>();
            foreach (var mindId in team.MemberMinds)
            {
                if (TryComp<MindComponent>(mindId, out var mindComp))
                    names.Add(mindComp.CharacterName ?? "Unknown");
            }
            var combinedName = string.Join(" + ", names);

            result.Add((team.MemberMinds[0], combinedName));
        }

        args.Minds = result;
        args.AgentName = Loc.GetString("blood-brother-roundend-name");
    }
}
