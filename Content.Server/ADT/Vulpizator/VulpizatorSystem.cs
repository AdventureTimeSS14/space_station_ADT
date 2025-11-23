using Content.Server.Antag;
using Content.Shared.Mindshield.Components;
using Content.Server.Polymorph.Components;
using Content.Shared.Projectiles;
using Content.Server.Roles;
using Content.Shared.Tag;
using Robust.Shared.Physics.Events;

namespace Content.Server.Vulpizator.System;

public sealed class VulpizatorSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    public const string _vulpa = "MobVulpkanin";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PolymorphedEntityComponent, StartCollideEvent>(OnPolymorphed);
    }
    private void OnPolymorphed(Entity<PolymorphedEntityComponent> uid, ref StartCollideEvent args)
    {
        if (uid.Comp.Configuration.Entity == _vulpa)
        {
            // Чтобы вульпе не приходило по 100 раз сообщение
            if (HasComp<RoleBriefingComponent>(uid))
            {
                return;
            }
            if (HasComp<MetaDataComponent>(uid))
            {
                _antag.SendBriefing(uid, Loc.GetString("vulpa-role-greeting"), Color.Red, null);
                EnsureComp<RoleBriefingComponent>(uid);
            }
            else if (HasComp<MindShieldComponent>(uid))
            {
                _antag.SendBriefing(uid, Loc.GetString("vulpa-role-mindshild"), Color.Red, null);
                EnsureComp<RoleBriefingComponent>(uid);
            }
        }
    }
}
