using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.Popups;
using Content.Shared.Electrocution;
using Content.Shared.ADT.ReliveResuscitation;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

using Content.Shared.DoAfter;

namespace Content.Server.ADT.ReliveResuscitation;

public sealed partial class ReliveResuscitationSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocutionSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReliveResuscitationComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerbs);
        //SubscribeLocalEvent<ReliveResuscitationComponent, ReliveDoAfterEvent>(ReliveEvent);
    }

    private void OnAltVerbs(EntityUid uid, ReliveResuscitationComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (TryComp<MobStateComponent>(uid, out var mobState) && TryComp<MetaDataComponent>(args.User, out var metaDataUser) && TryComp<MetaDataComponent>(uid, out var metaDataTarget))
        {
            if (mobState.CurrentState == MobState.Critical)
            {
                AlternativeVerb verbPersonalize = new()
                {
                    Act = () => Relive(uid, args.User, component, metaDataUser, metaDataTarget),
                    Text = Loc.GetString("Сердечно-лёгочная реанимация"),
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png")),
                };
                args.Verbs.Add(verbPersonalize);
            }
        }
    }

    private void Relive(EntityUid uid, EntityUid user, ReliveResuscitationComponent component, MetaDataComponent metaDataUser, MetaDataComponent metaDataTarget)
    {
        _popup.PopupEntity($"{metaDataUser.EntityName} делает сердечно-лёгочную реанимацию {metaDataTarget.EntityName}", uid, user);

        DoRelive(uid, user, component);
    }

    private void DoRelive(EntityUid uid, EntityUid user, ReliveResuscitationComponent component)
    {
        FixedPoint2 asphyxiationHeal = -20;
        FixedPoint2 bluntDamage = 5;

        var doAfterEventArgs =
            new DoAfterArgs(EntityManager, user, component.Delay, new ReliveDoAfterEvent(), uid, target: uid, used: user)
            {
                // Didn't break on damage as they may be trying to prevent it and
                // not being able to heal your own ticking damage would be frustrating.
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,

            };

        DamageSpecifier damageAsphyxiation = new(_prototypeManager.Index<DamageTypePrototype>("Asphyxiation"), asphyxiationHeal);
        DamageSpecifier damageBlunt = new(_prototypeManager.Index<DamageTypePrototype>("Blunt"), bluntDamage);

        _damageable.TryChangeDamage(uid, damageAsphyxiation, true);
        _damageable.TryChangeDamage(uid, damageBlunt, true);

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        //_doAfter.IsRunning(doAfterEventArgs);

        return;
    }
}
