// SPDX-FileCopyrightText: 2026 Kirill Golubenko
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT
//
// Author: Kirill Golubenko
// Discord: golub4ik
// Ckey: WikiHampter

using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.BloodBrothers.Components;

[RegisterComponent, Access(typeof(BloodBrotherRuleSystem))]
public sealed partial class BloodBrotherRuleComponent : Component
{
    public List<EntityUid> PendingMinds = new();

    public List<BloodBrotherTeam> Teams = new();

    [DataField]
    public List<string> MeetingAreas = new()
    {
        "blood-brothers-meeting-bar",
        "blood-brothers-meeting-dorms",
        "blood-brothers-meeting-arrivals",
        "blood-brothers-meeting-cargo",
        "blood-brothers-meeting-assistant",
        "blood-brothers-meeting-chapel",
        "blood-brothers-meeting-library",
    };

    [DataField]
    public List<EntProtoId> AvailableObjectives = new()
    {
        "BloodBrotherKillObjective",
        "BloodBrotherKillHeadObjective",
        "BloodBrotherStealCaptainIDObjective",
        "BloodBrotherStealCaptainGunObjective",
        "BloodBrotherStealRDHardsuitObjective",
        "BloodBrotherStealHyposprayObjective",
        "BloodBrotherStealMagbootsObjective",
        "BloodBrotherStealKravMagaObjective",
        "BloodBrotherStealHandTeleporterObjective",
    };

    [DataField]
    public ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";

    [DataField]
    public ProtoId<NpcFactionPrototype> SyndicateFaction = "Syndicate";

    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/ADT/BloodBrothers/blood_brothers_intro.ogg");
}

[DataDefinition]
public partial struct BloodBrotherTeam
{
    public List<EntityUid> MemberMinds;
    public string MeetingArea;
    public bool HasDocumentsObjective;
}
