using Content.Shared.ADT.Heretic.Components.Side;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Interaction;
using Content.Shared.MagicMirror;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
namespace Content.Server.ADT.Heretic.EntitySystems;
public sealed partial class HereticMirrorSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HereticMiraculousMirrorComponent, AfterInteractEvent>(OnInteract);
    }
    private void OnInteract(Entity<HereticMiraculousMirrorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;
        var user = args.User;
        if (!HasComp<MobStateComponent>(user))
            return;
        if (_heretic.IsHereticOrGhoul(user))
            return;
        args.Handled = true;
        _stun.TryUpdateParalyzeDuration(user, TimeSpan.FromSeconds(3));
        _popup.PopupEntity(Loc.GetString("heretic-mirror-trap"), user, user);
    }
}
