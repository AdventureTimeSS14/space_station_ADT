using Content.Server.Chat.Systems;
using Content.Server.Fax;
using Content.Server.Station.Systems;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Corvax.StationGoal
{
    /// <summary>
    ///     System to spawn paper with station goal.
    /// </summary>
    public sealed class StationGoalPaperSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly FaxSystem _fax = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!; // ADT-Tweak

        public override void Initialize()
        {
            SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        }

        private void OnRoundStarted(RoundStartedEvent ev)
        {
            if (!_cfg.GetCVar(CCCVars.StationGoal))
                return;

        // ADT-Tweak start
            foreach (var station in EntityQuery<StationGoalComponent>())
            {
                Timer.Spawn(TimeSpan.FromSeconds(station.SendDelay), () => SendRandomGoal(station.Owner, station));
            }
        }

        private void SendRandomGoal(EntityUid uid, StationGoalComponent station)
        {
            if (!Exists(uid))
                return;

            var playerCount = _playerManager.PlayerCount;
        // ADT-Tweak end

            var tempGoals = new List<ProtoId<StationGoalPrototype>>(station.Goals);
            StationGoalPrototype? selGoal = null;
            while (tempGoals.Count > 0)
            {
                var goalId = _random.Pick(tempGoals);
                var goalProto = _proto.Index(goalId);

                if (playerCount > goalProto.MaxPlayers ||
                    playerCount < goalProto.MinPlayers)
                {
                    tempGoals.Remove(goalId);
                    continue;
                }

                selGoal = goalProto;
                break;
            }

            if (selGoal is null)
                return;

            if (SendStationGoal(uid, selGoal))
            {
                Log.Info($"Goal {selGoal.ID} has been sent to station {MetaData(uid).EntityName}");
            }
        }

        public bool SendStationGoal(EntityUid ent, ProtoId<StationGoalPrototype> goal)
        {
            return SendStationGoal(ent, _proto.Index(goal));
        }

        /// <summary>
        ///     Send a station goal on selected station to all faxes which are authorized to receive it.
        /// </summary>
        /// <returns>True if at least one fax received paper</returns>
        public bool SendStationGoal(EntityUid ent, StationGoalPrototype goal)
        {
            var printout = new FaxPrintout(
                Loc.GetString(goal.Text, ("station", MetaData(ent).EntityName)),
                Loc.GetString("station-goal-fax-paper-name"),
                null,
                null,
                "paper_stamp-centcom",
                [new() { StampedName = Loc.GetString("stamp-component-stamped-name-centcom"), StampedColor = Color.FromHex("#006600") }]
            );

            var wasSent = false;
            var query = EntityQueryEnumerator<FaxMachineComponent>();
            while (query.MoveNext(out var faxUid, out var fax))
            {
                if (!fax.ReceiveAllStationGoals && !(fax.ReceiveStationGoal && _station.GetOwningStation(faxUid) == ent))
                    continue;

                _fax.Receive(faxUid, printout, null, fax);

                foreach (var spawnEnt in goal.Spawns)
                    SpawnAtPosition(spawnEnt, Transform(faxUid).Coordinates);

                wasSent |= fax.ReceiveStationGoal;
            }

            // ADT-Tweak start
            if (wasSent)
            {
                _chatSystem.DispatchGlobalAnnouncement(
                    Loc.GetString("adt-station-goal-sent-announcement", ("goal", Loc.GetString(goal.Name))),
                    Loc.GetString("centcom-random-event-sender"),
                    colorOverride: Color.Gold);
            }
            // ADT-Tweak end

            return wasSent;
        }
    }
}
