
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Slippery;
using Content.Shared.Verbs;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Content.Server.ADT.SlippingWake;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{

    // All smite verbs have names so invokeverb works.
    private void AddSmiteSlippingWakeVerb(GetVerbsEvent<Verb> args)
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
            var superslipNamee = Loc.GetString("admin-smite-speed-boost-name").ToLowerInvariant();
            Verb superslipp = new()
            {
                Text = superslipNamee,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new("ADT/Interface/Alerts/charge.rsi/charge4.png")),
                Act = () =>
                {
                    var hadSlipComponent = TryComp<SlippingWakeComponent>(args.Target, out var slipWakeComponent);

                    if (hadSlipComponent)
                    {
                        RemComp<SlippingWakeComponent>(args.Target);
                    }
                    else
                    {
                        EnsureComp<SlippingWakeComponent>(args.Target);
                    }
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", superslipNamee, Loc.GetString("admin-smite-speed-boost-description"))
            };
            args.Verbs.Add(superslipp);
        }

    }

}
