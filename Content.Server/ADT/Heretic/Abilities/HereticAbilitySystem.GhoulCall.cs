using Content.Shared.Heretic;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Localization;
using Content.Shared.Actions;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    private void OnGhoulCall(Entity<HereticComponent> ent, ref EventHereticGhoulCall args)
    {
        var hereticNet = GetNetEntity(ent.Owner);
        var hereticCoords = Transform(ent.Owner).Coordinates;
        var teleportedCount = 0;

        foreach (var ghoulEnt in EntityManager.AllEntities<GhoulComponent>())
        {
            if (ghoulEnt.Comp.BoundHeretic == hereticNet)
            {
                teleportedCount++;
            }
        }

        if (teleportedCount == 0)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ghoul-call-no-ghouls"), ent.Owner, PopupType.Medium);
            return;
        }

        if (!TryUseAbility(ent, args))
            return;

        foreach (var ghoulEnt in EntityManager.AllEntities<GhoulComponent>())
        {
            if (ghoulEnt.Comp.BoundHeretic == hereticNet)
            {
                _transform.SetCoordinates(ghoulEnt.Owner, hereticCoords);
            }
        }

        _popup.PopupEntity(Loc.GetString("heretic-ghoul-call-success", ("count", teleportedCount)), ent.Owner, PopupType.Large);
        _aud.PlayPvs(new SoundPathSpecifier("/Audio/ADT/Heretic/voidblink.ogg"), ent.Owner);
        args.Handled = true;
    }
}
