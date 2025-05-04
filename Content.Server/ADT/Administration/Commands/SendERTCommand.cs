using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Map;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Content.Server.Chat.Managers;
using Robust.Shared.ContentPack;
using System.Threading.Tasks;
using Robust.Shared.Utility;

using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;

namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SendERTCommand : IConsoleCommand
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IEntitySystemManager _system = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;

    public string Command => "sendert";
    public string Description => Loc.GetString("send-ert-description");
    public string Help => Loc.GetString("send-ert-help");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        #region Setup vars
        string audioPath = "";
        string defaultGridPath = "/Maps/ADTMaps/Shuttles/ERT", defaultAudioPath = "/Audio/Corvax/Adminbuse";
        string alertLevelCode = "gamma";
        int volume = 0;
        bool isLoadGrid = false, isAnnounce = true, isPlayAudio = true, isSetAlertLevel = true, playAuidoFromAnnouncement = false;
        Color announceColor = Color.SeaBlue;
        #endregion

        var player = shell.Player;
        if (player?.AttachedEntity == null) // Are we the server's console?
        { shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command")); return; }

        var stationUid = _system.GetEntitySystem<StationSystem>().GetOwningStation(player.AttachedEntity.Value);
        if (stationUid == null)
        {
            shell.WriteLine(Loc.GetString("cmd-setalertlevel-invalid-grid"));
            return;
        }

        #region Set isAnnounce
        switch (args.Length)
        {
            case 0:
                shell.WriteLine(Loc.GetString("send-ert-help"));
                return;
			case 1:
                isAnnounce = true;
				break;
            default:
                if (bool.TryParse(args[1].ToLower(), out var temp)) { isAnnounce = temp; }
                else { shell.WriteError(Loc.GetString($"send-ert-truefalse-error")); return; }
                break;
        }
        #endregion

        #region ERT type check
        switch (args[0].ToLower())
        {
            case "default":
                audioPath = $"{defaultAudioPath}/yesert.ogg";
                isLoadGrid = true;
                break;

            case "default-rev":
                audioPath = $"{defaultAudioPath}/yesert.ogg";
                isLoadGrid = true;
                break;

            case "security":
                audioPath = $"{defaultAudioPath}/yesert.ogg";
                isLoadGrid = true;
                break;

            case "engineer":
                audioPath = $"{defaultAudioPath}/yesert.ogg";
                isLoadGrid = true;
                break;

            case "medical":
                audioPath = $"{defaultAudioPath}/yesert.ogg";
                isLoadGrid = true;
                break;

            case "janitor":
                audioPath = $"{defaultAudioPath}/yesert.ogg";
                isLoadGrid = true;
                break;

            case "chaplain":
                audioPath = $"{defaultAudioPath}/yesert.ogg";
                isLoadGrid = true;
                break;

            case "cbun":
                audioPath = $"{defaultAudioPath}/yesert.ogg";
                isLoadGrid = true;
                break;

            case "deathsquad":
                //alertLevelCode = "epsilon";
                announceColor = Color.White;
                isLoadGrid = true;
                break;

            case "denial":
                audioPath = $"{defaultAudioPath}/noert.ogg";
                isAnnounce = true;
                isLoadGrid = false;
                isSetAlertLevel = false;
                break;

            default:
                isLoadGrid = false;
                shell.WriteError(Loc.GetString("send-ert-erttype-error"));
                return;

        }
        #endregion

        #region Command's body
        if (isLoadGrid) // Create grid & map
        {
            var gridPath = $"{defaultGridPath}/{args[0].ToLower()}.yml";
            if (!_resourceManager.TryContentFileRead(gridPath, out var _))
            {
                shell.WriteError(Loc.GetString("send-ert-erttype-error-path"));
                shell.WriteError($"No map found: {gridPath}");
                return;
            }

            var mapId = _mapManager.CreateMap();
            _system.GetEntitySystem<MetaDataSystem>().SetEntityName(_mapManager.GetMapEntityId(mapId), Loc.GetString("sent-ert-map-name"));
            var opts = new DeserializationOptions {StoreYamlUids = true, InitializeMaps = true};
            _system.GetEntitySystem<MapLoaderSystem>().TryLoadGrid(mapId, new ResPath(gridPath), out _, opts);
            shell.WriteLine($"Карта {gridPath} успешно загружена! :з");
            _chat.SendAdminAlert($"Админ {player.Name} вызвал {args[0].ToLower()}. Карте 'Сектор патрулирования' было присовино ID: {mapId}. Точка телепортации для призраков появилась на шаттле.");
        }

        if (isAnnounce) // Write announce & play audio
        {
            if (isPlayAudio)
            {
                Filter filter = Filter.Empty().AddAllPlayers(_playerManager);

                var audioOption = AudioParams.Default;
                audioOption = audioOption.WithVolume(volume);

                _entManager.System<ServerGlobalSoundSystem>().PlayAdminGlobal(filter, audioPath, audioOption, true);
            }

            _system.GetEntitySystem<ChatSystem>().DispatchGlobalAnnouncement(Loc.GetString($"ert-send-{args[0].ToLower()}-announcement"), Loc.GetString($"ert-send-{args[0].ToLower()}-announcer"), playSound: playAuidoFromAnnouncement, colorOverride: announceColor);

            if (args[0].ToLower() != "denial") // Если отказ то задержка нам не нужна
                await Task.Delay(10000); // Ставим задержку на 10 секунд

            if (isSetAlertLevel)
            {
                if (stationUid == null) { shell.WriteLine(Loc.GetString("sent-ert-invalid-grid")); return; } //We are on station?
                _system.GetEntitySystem<AlertLevelSystem>().SetLevel(stationUid.Value, alertLevelCode, false, true, true, true);
            }
        }
        #endregion

        _adminLogger.Add(LogType.Action, LogImpact.High, $"{player} send ERT. Type: {args[0]}. Is announce: {isAnnounce}");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var type = new CompletionOption[]
            {
                new("Default", Loc.GetString("send-ert-hint-type-default")),
                new("Default-rev", Loc.GetString("send-ert-hint-type-default-rev")),
                new("Security", Loc.GetString("send-ert-hint-type-security")),
                new("Engineer", Loc.GetString("send-ert-hint-type-engineer")),
                new("Medical", Loc.GetString("send-ert-hint-type-medical")),
                new("Janitor", Loc.GetString("send-ert-hint-type-janitor")),
                new("Chaplain", Loc.GetString("send-ert-hint-type-chaplain")),
                new("CBUN", Loc.GetString("send-ert-hint-type-cbrn")),
                new("DeathSquad", Loc.GetString("send-ert-hint-type-deathsquad")),
                new("Denial", Loc.GetString("send-ert-hint-type-denial")),
            };
            return CompletionResult.FromHintOptions(type, Loc.GetString("send-ert-hint-type"));
        }

        if (args.Length == 2)
        {
            var isAnnounce = new CompletionOption[]
            {
                new("true", Loc.GetString("send-ert-hint-isannounce-true")),
                new("false", Loc.GetString("send-ert-hint-isannounce-false")),
            };
            return CompletionResult.FromHintOptions(isAnnounce, Loc.GetString("send-ert-hint-isannounce"));
        }

        return CompletionResult.Empty;
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
