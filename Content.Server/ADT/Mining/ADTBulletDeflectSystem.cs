using Content.Shared.ADT.Mining;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.Mining;

public sealed class ADTBulletDeflectSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ADTBulletDeflectComponent, ProjectileReflectAttemptEvent>(OnReflectAttempt);
    }

    private void OnReflectAttempt(Entity<ADTBulletDeflectComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        //  туду переделатель в настоящий рефлект
        if (args.Component.PenetrationAmount >= ent.Comp.RequiredPenetration)
            return;

        args.Cancelled = true;

        _popup.PopupEntity(Loc.GetString(ent.Comp.Popup, ("creature", Identity.Name(ent.Owner, EntityManager))), ent);
        _audio.PlayPvs(ent.Comp.Sound, ent);
    }
}
