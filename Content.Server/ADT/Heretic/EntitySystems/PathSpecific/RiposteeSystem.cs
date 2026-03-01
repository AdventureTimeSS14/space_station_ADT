using Content.Server.Heretic.Components.PathSpecific;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Server.Hands.Systems;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using System.Linq;

namespace Content.Server.Heretic.EntitySystems.PathSpecific;

public sealed partial class RiposteeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RiposteeComponent, BeforeDamageChangedEvent>(OnRiposteeBeforeDamage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var eqe = EntityQueryEnumerator<RiposteeComponent>();
        while (eqe.MoveNext(out var uid, out var rip))
        {
            if (!uid.IsValid())
                continue;

            if (rip.CanRiposte)
                continue;

            rip.Timer -= frameTime;

            if (rip.Timer <= 0)
            {
                rip.Timer = rip.Cooldown;

                rip.CanRiposte = true;
                _popup.PopupCursor(Loc.GetString("heretic-riposte-available"), uid);
            }
        }
    }

    private void OnRiposteeBeforeDamage(EntityUid uid, RiposteeComponent riposte, ref BeforeDamageChangedEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        if (!riposte.CanRiposte)
            return;

        if (args.Origin == null || args.Origin == uid)
            return;

        if (!HasTwoHandedWeaponOrDualWield(uid))
            return;

        args.Cancelled = true;

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg"), uid);

        riposte.CanRiposte = false;
        _popup.PopupCursor(Loc.GetString("heretic-riposte-used"), uid);
    }

    private bool HasTwoHandedWeaponOrDualWield(EntityUid ent)
    {
        if (!TryComp<HandsComponent>(ent, out var handsComp))
            return false;

        var meleeWeapons = new List<EntityUid>();

        foreach (var handName in handsComp.SortedHands)
        {
            if (_hands.TryGetHeldItem((ent, handsComp), handName, out var heldItem))
            {
                if (HasComp<MeleeWeaponComponent>(heldItem))
                    meleeWeapons.Add(heldItem.Value);
            }
        }

        return meleeWeapons.Count >= 2;
    }
}
