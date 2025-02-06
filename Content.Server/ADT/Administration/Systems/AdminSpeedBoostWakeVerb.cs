using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Slippery;
using Content.Shared.Verbs;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Content.Server.ADT.SpeedBoostWake;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{

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

    }

}
