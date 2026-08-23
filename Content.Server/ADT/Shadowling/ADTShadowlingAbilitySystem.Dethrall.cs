using Content.Shared.ADT.Shadowling;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Flash.Components;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.ADT.Silicon.Components;
using Content.Shared.Standing;

namespace Content.Server.ADT.Shadowling;

public sealed partial class ADTShadowlingAbilitySystem
{
    private void InitializeDethrall()
    {
        SubscribeLocalEvent<ADTShadowlingThrallComponent, InteractUsingEvent>(OnThrallInteractUsing);
        SubscribeLocalEvent<ADTShadowlingThrallComponent, ADTShadowlingDethrallDoAfterEvent>(OnDethrallDoAfter);
    }

    private void OnThrallInteractUsing(Entity<ADTShadowlingThrallComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !IsLightSource(args.Used))
            return;

        if (_standing.IsDown(ent.Owner) || HasComp<BorgChassisComponent>(ent) || HasComp<SiliconComponent>(ent))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-dethrall-begin", ("target", ent.Owner)), args.User, args.User);

            var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.DethrallTime, new ADTShadowlingDethrallDoAfterEvent(), ent, ent, args.Used)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = true,
            };

            _doAfter.TryStartDoAfter(doAfter);
            args.Handled = true;
            return;
        }

        _popup.PopupEntity(Loc.GetString("shadowling-dethrall-needs-down"), args.User, args.User);
    }

    private void OnDethrallDoAfter(Entity<ADTShadowlingThrallComponent> ent, ref ADTShadowlingDethrallDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        if (TryComp<ADTLesserShadowlingComponent>(ent, out var lesser))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-dethrall-backlash-thrall"), ent, ent, PopupType.LargeCaution);
            _popup.PopupEntity(Loc.GetString("shadowling-dethrall-backlash-surgeon", ("target", ent.Owner)), args.User, args.User, PopupType.LargeCaution);

            _stun.TryKnockdown(args.User, lesser.SurgeryBacklashStun, true);

            var backlash = new DamageSpecifier();
            backlash.DamageDict.Add("Blunt", lesser.SurgeryBacklashDamage);
            _damageable.TryChangeDamage(args.User, backlash, true, origin: ent);
            return;
        }

        var tumorProto = ent.Comp.TumorProto;

        if (!_shadowling.TryFreeThrall(ent))
            return;

        Spawn(tumorProto, Transform(ent).Coordinates);
        _popup.PopupEntity(Loc.GetString("shadowling-dethrall-success", ("target", ent.Owner)), args.User, args.User, PopupType.Medium);
        _popup.PopupEntity(Loc.GetString("shadowling-dethrall-freed"), ent, ent, PopupType.LargeCaution);
    }

    private bool IsLightSource(EntityUid uid)
    {
        return HasComp<HandheldLightComponent>(uid) || HasComp<FlashComponent>(uid);
    }
}
