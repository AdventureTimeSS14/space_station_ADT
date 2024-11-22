using System.Linq;
using System.Numerics;
using Content.Server.Administration.Commands;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Preferences.Managers;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Server.GameTicking.Rules;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.GameTicking.Components;
using Content.Server.StationEvents.Components;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// This handles the Pirates minor antag, which is designed to coincide with other modes on occasion.
/// </summary>
public sealed class PiratesRuleSystem : GameRuleSystem<PiratesRuleComponent>
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly PricingSystem _pricingSystem = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

    }

    protected override void Started(EntityUid uid, PiratesRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var shuttleMap = _mapManager.CreateMap();
        var options = new MapLoadOptions
        {
            LoadMap = true,
        };

        if (!_map.TryLoad(shuttleMap, component.PiratesShuttlePath, out var shuttle, options))
            return;
        component.PirateShip = shuttle[0];

        component.InitialShipValue = _pricingSystem.AppraiseGrid(component.PirateShip, uid =>
        {
            component.InitialItems.Add(uid);
            return true;
        });

    }
    protected override void AppendRoundEndText(EntityUid uid, PiratesRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        if (Deleted(component.PirateShip))
        {
            // Major loss, the ship somehow got annihilated.
            args.AddLine(Loc.GetString("pirates-no-ship"));
        }
        else
        {
            List<(double, EntityUid)> mostValuableThefts = new();
            var comp1 = component;
            var finalValue = _pricingSystem.AppraiseGrid(component.PirateShip, uid =>
            {
                foreach (var mindId in component.Pirates)
                {
                    if (TryComp(mindId, out MindComponent? mind) && mind.CurrentEntity == uid)
                        return false; // Don't appraise the pirates twice, we count them in separately.
                }

                return true;
            }, (uid, price) =>
            {
                if (comp1.InitialItems.Contains(uid))
                    return;
                mostValuableThefts.Add((price, uid));
                mostValuableThefts.Sort((i1, i2) => i2.Item1.CompareTo(i1.Item1));
                if (mostValuableThefts.Count > 5)
                    mostValuableThefts.Pop();
            });

            foreach (var mindId in component.Pirates)
            {
                if (TryComp(mindId, out MindComponent? mind) && mind.CurrentEntity is not null)
                    finalValue += _pricingSystem.GetPrice(mind.CurrentEntity.Value);
            }

            var score = finalValue - component.InitialShipValue;

            args.AddLine(Loc.GetString("pirates-final-score", ("score", $"{score:F2}")));
            args.AddLine(Loc.GetString("pirates-final-score-2", ("finalPrice", $"{finalValue:F2}")));

            args.AddLine("");
            args.AddLine(Loc.GetString("pirates-most-valuable"));

            foreach (var (price, obj) in mostValuableThefts)
            {
                args.AddLine(Loc.GetString("pirates-stolen-item-entry", ("entity", obj), ("credits", $"{price:F2}")));
            }

            if (mostValuableThefts.Count == 0)
                args.AddLine(Loc.GetString("pirates-stole-nothing"));
        }
    }
}
