// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Linq;
using System.Text;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Shared.Antag;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;

namespace Content.Server.GameTicking.Rules
{
    public sealed partial class VampireRuleSystem : GameRuleSystem<VampireRuleComponent>
    {
        [Dependency] private AntagSelectionSystem _antag = default!;
        [Dependency] private SharedMindSystem _mind = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VampireRuleComponent, AfterAntagEntitySelectedEvent>(OnVampireSelected);
            SubscribeLocalEvent<VampireRoleComponent, GetBriefingEvent>(OnVampireBriefing);
        }

        private void OnVampireSelected(Entity<VampireRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
        {
            var ent = args.EntityUid;
            _antag.SendBriefing(ent, MakeBriefing(ent), Color.Purple, null);
        }

        private void OnVampireBriefing(Entity<VampireRoleComponent> vampire, ref GetBriefingEvent args)
        {
            var ent = args.Mind.Comp.OwnedEntity;
            if (ent is null)
                return;

            args.Append(MakeBriefing(ent.Value));
        }

        private string MakeBriefing(EntityUid ent)
        {
            var briefing = HasComp<HumanoidProfileComponent>(ent)
                ? Loc.GetString("vampire-role-greeting-human")
                : Loc.GetString("vampire-role-greeting-animal");

            return briefing;
        }

        protected override void AppendRoundEndText(EntityUid uid,
            VampireRuleComponent component,
            GameRuleComponent gameRule,
            ref RoundEndTextAppendEvent args)
        {
            if (component.VampiresInfo.Count == 0)
                return;

            var sb = new StringBuilder();
            sb.AppendLine(Loc.GetString("vampire-round-end-header"));

            foreach (var (_, info) in component.VampiresInfo)
            {
                var name = !string.IsNullOrEmpty(info.Name) ? info.Name : Loc.GetString("generic-unknown");
                var className = Loc.GetString($"select-class-{info.Class.ToString().ToLower()}");
                var bloodAmount = info.TotalBloodDrank.Float().ToString("F2");
                var classColor = GetClassColor(info.Class);

                var line = Loc.GetString("vampire-round-end-info", ("name", name), ("class", className),
                    ("blood", bloodAmount), ("color", classColor));

                sb.AppendLine(line);
            }

            sb.AppendLine();
            var totalBloodDrank = GetTotalBloodDrankInRound(component).ToString("F2");
            sb.AppendLine(Loc.GetString("vampires-drank-total-blood", ("bloodAmount", totalBloodDrank)));

            args.AddLine(sb.ToString());
        }

        private Color GetClassColor(VampireClassEnum vampireClass)
        {
            return vampireClass switch
            {
                VampireClassEnum.Hemomancer => Color.FromHex("#b82e2e"),
                VampireClassEnum.Umbrae => Color.FromHex("#6709aa"),
                VampireClassEnum.Gargantua => Color.FromHex("#b34019"),
                VampireClassEnum.Dantalion => Color.FromHex("#2a9633"),
                VampireClassEnum.Bestia => Color.FromHex("#2770c4"),
                _ => Color.Yellow
            };
        }

        private float GetTotalBloodDrankInRound(VampireRuleComponent component)
        {
            var totalBloodDrank = 0f;
            foreach (var (_, info) in component.VampiresInfo)
                totalBloodDrank += info.TotalBloodDrank.Float();

            return totalBloodDrank;
        }

        #region Records

        public void InitVampireRecord(EntityUid vampireUid, VampireComponent? vampireComp = null)
        {
            if (!Resolve(vampireUid, ref vampireComp, false))
                return;

            var rule = EntityQuery<VampireRuleComponent>().FirstOrDefault();
            if (rule == null)
                return;

            var mindId = _mind.GetMind(vampireUid);
            if (mindId == null)
                return;

            if (rule.VampiresInfo.ContainsKey(mindId.Value))
                return;

            var info = new VampireRoundInfo
            {
                Name = Name(vampireUid)
            };

            rule.VampiresInfo[mindId.Value] = info;
        }

        public void RecordBloodDrank(EntityUid vampireUid, FixedPoint2 amount)
        {
            if (amount <= 0)
                return;

            var rule = EntityQuery<VampireRuleComponent>().FirstOrDefault();
            if (rule == null)
                return;

            var mindId = _mind.GetMind(vampireUid);
            if (mindId == null)
                return;

            if (!rule.VampiresInfo.TryGetValue(mindId.Value, out var info))
            {
                info = new VampireRoundInfo
                {
                    Name = Name(vampireUid),
                    Class = CompOrNull<VampireComponent>(vampireUid)?.CurrentEvolution ?? default
                };
                rule.VampiresInfo[mindId.Value] = info;
            }

            info.TotalBloodDrank += amount;
        }

        public void RecordClassSelected(EntityUid vampireUid, VampireClassEnum selectedClass)
        {
            var rule = EntityQuery<VampireRuleComponent>().FirstOrDefault();
            if (rule == null)
                return;

            var mindId = _mind.GetMind(vampireUid);
            if (mindId == null)
                return;

            if (!rule.VampiresInfo.TryGetValue(mindId.Value, out var info))
            {
                info = new VampireRoundInfo
                {
                    Name = Name(vampireUid),
                    Class = selectedClass
                };
                rule.VampiresInfo[mindId.Value] = info;
            }
            else
            {
                info.Class = selectedClass;
            }
        }

        #endregion
    }
}
