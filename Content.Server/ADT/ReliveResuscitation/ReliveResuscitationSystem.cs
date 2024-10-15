using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.Popups;
using Content.Shared.ADT.ReliveResuscitation;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.DoAfter;


namespace Content.Server.ADT.ReliveResuscitation;

public sealed partial class ReliveResuscitationSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReliveResuscitationComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerbs);
        SubscribeLocalEvent<ReliveResuscitationComponent, ReliveDoAfterEvent>(DoRelive);
    }

    private void OnAltVerbs(EntityUid uid, ReliveResuscitationComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (TryComp<MobStateComponent>(uid, out var mobState))
        {
            if (mobState.CurrentState == MobState.Critical)
            {
                AlternativeVerb verbPersonalize = new()
                {
                    Act = () => Relive(uid, args.User, component, mobState),
                    Text = Loc.GetString("Сердечно-лёгочная реанимация"),
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png")),
                };
                args.Verbs.Add(verbPersonalize);
            }
        }
    }

    private void Relive(EntityUid uid, EntityUid user, ReliveResuscitationComponent component, MobStateComponent mobState)
    {
        var stringLoc = Loc.GetString("relive-start-message", ("user", Identity.Entity(user, EntityManager)), ("name", Identity.Entity(uid, EntityManager)));
        _popup.PopupEntity(stringLoc, uid, user);
        var doAfterEventArgs =
            new DoAfterArgs(EntityManager, user, component.Delay, new ReliveDoAfterEvent(), uid, target: uid, used: user)
            {
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
                BreakOnDamage = true,
            };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }
    private void DoRelive(EntityUid uid, ReliveResuscitationComponent component, ref ReliveDoAfterEvent args)
    {
        if (TryComp<MobStateComponent>(uid, out var mobState) && mobState.CurrentState == MobState.Critical)
        {
            if (args.Handled || args.Cancelled)
                return;

            FixedPoint2 asphyxiationHeal = -20;
            FixedPoint2 bluntDamage = 3;
            DamageSpecifier damageAsphyxiation = new(_prototypeManager.Index<DamageTypePrototype>("Asphyxiation"), asphyxiationHeal);
            DamageSpecifier damageBlunt = new(_prototypeManager.Index<DamageTypePrototype>("Blunt"), bluntDamage);
            _damageable.TryChangeDamage(uid, damageAsphyxiation, true);
            _damageable.TryChangeDamage(uid, damageBlunt, true);

            args.Handled = true;
            args.Repeat = true;
        }
    }
}
