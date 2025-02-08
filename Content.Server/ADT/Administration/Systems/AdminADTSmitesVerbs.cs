using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Content.Server.ADT.SpeedBoostWake;
using Content.Shared.Movement.Components;
using Content.Shared.StatusEffect;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    // All smite verbs have names so invokeverb works.
    private void AddSmiteSpeedBoostWakeVerb(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        // 1984.
        if (HasComp<MapComponent>(args.Target) || HasComp<MapGridComponent>(args.Target))
            return;

        if (TryComp<PhysicsComponent>(args.Target, out var physics))
        {
            var superBoostSpeedName = Loc.GetString("admin-smite-speed-boost-name").ToLowerInvariant();
            Verb superBoostSpeed = new()
            {
                Text = superBoostSpeedName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new("ADT/Interface/Alerts/charge.rsi/charge4.png")),
                Act = () =>
                {
                    var hadSlipComponent = TryComp<SpeedBoostWakeComponent>(args.Target, out var _);

                    if (hadSlipComponent)
                        RemComp<SpeedBoostWakeComponent>(args.Target);
                    else
                        EnsureComp<SpeedBoostWakeComponent>(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", superBoostSpeedName, Loc.GetString("admin-smite-speed-boost-description"))
            };
            args.Verbs.Add(superBoostSpeed);
        }

        // Created by d_bamboni for schrodinger71
        if (TryComp<InputMoverComponent>(args.Target, out var _))
        {
            Verb divineDelay = new()
            {
                Text = Loc.GetString("admin-smite-divine-delay-name"),
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new("/Textures/ADT/Interface/Alerts/AdminSmite/divineDelay.png")),
                Act = () =>
                {
                    var xform = Transform(args.Target);
                    foreach (var entity in _entityLookup.GetEntitiesInRange(xform.Coordinates, 10f))
                    {
                        if (TryComp<InputMoverComponent>(entity, out var _))
                        {
                            _electrocutionSystem.TryDoElectrocution(entity, null, 50, TimeSpan.FromSeconds(15), refresh: true, ignoreInsulation: true);
                            Spawn("Lightning",Transform(entity).Coordinates);
                        }
                    }
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-divine-delay-description")
            };
            args.Verbs.Add(divineDelay);
        }

    }

}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝
*/
