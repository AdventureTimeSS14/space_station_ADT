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
using Content.Shared.Actions;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Content.Shared.ADT.Phantom;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Content.Shared.Movement.Events;
using Content.Server.Power.EntitySystems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using Content.Shared.IdentityManagement;
using Content.Shared.Chat;
using Content.Server.Chat;
using System.Linq;
using Content.Shared.Heretic;
using Content.Shared.Alert;
using Robust.Server.GameObjects;
using Content.Server.Chat.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.StatusEffect;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Humanoid;
using Robust.Shared.Containers;
using Content.Shared.DoAfter;
using System.Numerics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Server.Chat.Managers;
using Robust.Shared.Prototypes;
using Content.Shared.Stunnable;
using Content.Server.Power.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Timing;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Bible.Components;
using Content.Server.Body.Systems;
using Content.Server.Station.Systems;
using Content.Server.EUI;
using Content.Server.Body.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Server.ADT.Hallucinations;
using Content.Server.AlertLevel;
using Content.Shared.ADT.Controlled;
using Robust.Shared.Audio.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.CombatMode;
using Content.Server.Cuffs;
using Robust.Server.Player;
using Content.Shared.Preferences;
using Content.Shared.Ghost;
using Content.Shared.Tag;
using Content.Server.Hands.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.ADT.Hallucinations;
using Robust.Shared.Utility;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.ADT.GhostInteractions;
using Content.Shared.Revenant.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.ADT.Silicon.Components;
using Content.Server.Singularity.Events;
using Content.Server.GameTicking.Rules;

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
        SubscribeLocalEvent<ReliveResuscitationComponent, ReliveDoAfterEvent>(DoRelive);
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
        //var stringLoc = Loc.GetString("relive-start-message", ("user", Identity.Entity(user, EntityManager)), ("name", Identity.Entity(uid, EntityManager)));
        _popup.PopupEntity($"{metaDataUser.EntityName} делает сердечно-лёгочную реанимацию {metaDataTarget.EntityName}", uid, user);
        var doAfterEventArgs =
            new DoAfterArgs(EntityManager, user, component.Delay, new ReliveDoAfterEvent(), uid, target: uid, used: user)
            {
                // Didn't break on damage as they may be trying to prevent it and
                // not being able to heal your own ticking damage would be frustrating.
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
                BreakOnDamage = true,
            };
        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }
    private void DoRelive(EntityUid uid, ReliveResuscitationComponent component, ref ReliveDoAfterEvent agrs)
    {
        FixedPoint2 asphyxiationHeal = -20;
        FixedPoint2 bluntDamage = 5;
        DamageSpecifier damageAsphyxiation = new(_prototypeManager.Index<DamageTypePrototype>("Asphyxiation"), asphyxiationHeal);
        DamageSpecifier damageBlunt = new(_prototypeManager.Index<DamageTypePrototype>("Blunt"), bluntDamage);
        _damageable.TryChangeDamage(uid, damageAsphyxiation, true);
        _damageable.TryChangeDamage(uid, damageBlunt, true);
    }
}
