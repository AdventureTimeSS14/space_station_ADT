using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Database;
using Content.Shared.Flash;
using Content.Shared.Humanoid;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Zombies;
using Content.Server.ADT.Roles;
using Content.Shared.ADT.BloodBrothers;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.BloodBrothers;

public sealed class BloodBrotherSystem : SharedBloodBrothersSystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    public ProtoId<NpcFactionPrototype> BloodBrotherFaction = "BloodBrotherFaction";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherLeaderComponent, AfterFlashedEvent>(OnPostFlash);
    }
    private void OnPostFlash(EntityUid uid, BloodBrotherLeaderComponent comp, ref AfterFlashedEvent ev)
    {
        if (uid != ev.User || !ev.Melee)
            return;
        if (!_mind.TryGetMind(ev.Target, out var mindId, out var mind))
            return;

        if (HasComp<BloodBrotherComponent>(ev.Target) ||
            HasComp<MindShieldComponent>(ev.Target) ||
            !HasComp<HumanoidAppearanceComponent>(ev.Target) ||
            !_mobState.IsAlive(ev.Target) ||
            HasComp<ZombieComponent>(ev.Target) ||
            comp.MaxConvertedCount <= comp.ConvertedCount)
        {
            return;
        }

        _npcFaction.AddFaction(ev.Target, BloodBrotherFaction);
        EnsureComp<BloodBrotherComponent>(ev.Target, out var brocomp);
        brocomp.Leader = uid;
        if (ev.User != null)
        {
            _adminLogManager.Add(LogType.Mind,
                LogImpact.Medium,
                $"{ToPrettyString(ev.User.Value)} converted {ToPrettyString(ev.Target)} into a brotherhood");
        }

        if (mindId == default || !_role.MindHasRole<BloodBrotherRoleComponent>(mindId))
        {
            _role.MindAddRole(mindId, "MindRoleBloodBrother");
        }

        _antag.SendBriefing(ev.Target, Loc.GetString("brother-briefing"), Color.Red, comp.StartSound);
        comp.ConvertedCount++;
        Dirty(ev.Target, brocomp);
    }
}
