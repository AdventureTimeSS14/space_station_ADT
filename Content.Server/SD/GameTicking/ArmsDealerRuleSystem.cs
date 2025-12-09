using Content.Server.Antag;
using Content.Server._SD.GameTicking.Components;
using Content.Server._SD.Roles;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Roles;
using Content.Shared.Humanoid;
using Content.Server._SD.Roles;

namespace Content.Server._SD.GameTicking.Rules;

public sealed class ArmsDealerRuleSystem : GameRuleSystem<ArmsDealerRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmsDealerRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);

        SubscribeLocalEvent<ArmsDealerRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void AfterAntagSelected(Entity<ArmsDealerRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
    {
        var ent = args.EntityUid;
        _antag.SendBriefing(ent, MakeBriefing(ent), null, null);
    }

    private void OnGetBriefing(Entity<ArmsDealerRoleComponent> role, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;

        if (ent is null)
            return;
        args.Append(MakeBriefing(ent.Value));
    }

    private string MakeBriefing(EntityUid ent)
    {
        var isHuman = HasComp<HumanoidAppearanceComponent>(ent);
        var briefing = isHuman
            ? Loc.GetString("arms-dealer-role-greeting-human")
            : Loc.GetString("arms-dealer-role-greeting-animal");

        if (isHuman)
            briefing += "\n \n" + Loc.GetString("arms-dealer-role-greeting-equipment") + "\n";

        return briefing;
    }
}
