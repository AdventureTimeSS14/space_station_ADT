using System.Linq;
using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Content.Server.Chat.Systems;
using Serilog;

namespace Content.Server.Corvax.StationGoal
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class StationGoalCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "sendstationgoal";
        public string Description => Loc.GetString("send-station-goal-command-description");
        public string Help => Loc.GetString("send-station-goal-command-help-text", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!NetEntity.TryParse(args[0], out var euidNet) || !_entManager.TryGetEntity(euidNet, out var euid))
            {
                shell.WriteError($"Failed to parse euid '{args[0]}'.");
                return;
            }

            var protoId = args[1];
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            if (!prototypeManager.TryIndex<StationGoalPrototype>(protoId, out var proto))
            {
                shell.WriteError($"No station goal found with ID {protoId}!");
                return;
            }

            var stationGoalPaper = IoCManager.Resolve<IEntityManager>().System<StationGoalPaperSystem>();
            if (!stationGoalPaper.SendStationGoal(euid, protoId))
            {
                shell.WriteError("Station goal was not sent");
                return;
            }
            // ADT-Tweak-start
            // if (!_entManager.TryGetComponent<MetaDataComponent>(euid, out var station))
            //     return;
            // string stationName = station.EntityName;

            string nameStationPretty = _entManager.ToPrettyString(euid);
            var nameParts = nameStationPretty.Split('(');
            var stationName = nameParts[0].Trim(); // Получаем имя станциями с помощью махинаций по строкам

            var chat = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();
            chat.DispatchGlobalAnnouncement(
                Loc.GetString("station-goal-announcement", ("station", stationName), ("goal", Loc.GetString(proto.ID))),
                sender: Loc.GetString("station-goal-announcement-CentCom"),
                colorOverride: Color.Yellow);
            // ADT-Tweak-end
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    var stations = ContentCompletionHelper.StationIds(_entManager);
                    return CompletionResult.FromHintOptions(stations, "[StationId]");
                case 2:
                    var options = IoCManager.Resolve<IPrototypeManager>()
                        .EnumeratePrototypes<StationGoalPrototype>()
                        .Select(p => new CompletionOption(p.ID));

                    return CompletionResult.FromHintOptions(options, Loc.GetString("send-station-goal-command-arg-id"));
            }
            return CompletionResult.Empty;
        }
    }
}
