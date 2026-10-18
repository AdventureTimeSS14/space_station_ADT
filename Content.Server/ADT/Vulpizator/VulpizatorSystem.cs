using Content.Server.Antag;
using Content.Server.Polymorph.Components;
using Content.Shared.Mindshield;
using Content.Shared.Roles.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.Vulpizator.System;

public sealed class VulpizatorSystem : EntitySystem
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private MindShieldSystem _mindShield = default!;
    public const string Vulpa = "ADTMobRandomVulpkanin";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PolymorphedEntityComponent, StartCollideEvent>(OnPolymorphed);
    }
    private void OnPolymorphed(Entity<PolymorphedEntityComponent> uid, ref StartCollideEvent args)
    {
        if (uid.Comp.Configuration.Entity == Vulpa)
        {
            // Чтобы вульпе не приходило по 100 раз сообщение
            if (HasComp<RoleBriefingComponent>(uid))
            {
                return;
            }
            _mindShield.GetMindshieldStatus(uid, out var isMindshielded, out _);
            if (isMindshielded)
            {
                _antag.SendBriefing(uid, Loc.GetString("vulpa-role-greeting-mindshild"), Color.Red, null);
                EnsureComp<RoleBriefingComponent>(uid);
            }
            else
            {
                _antag.SendBriefing(uid, Loc.GetString("vulpa-role-greeting"), Color.Red, null);
                EnsureComp<RoleBriefingComponent>(uid);
            }
        }
    }
}
