using Content.Server.Administration.Commands;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Zombies;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.UI;
using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Tube.Components;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Server.Mind;
using Content.Server.Mind.Commands;
using Content.Server.Prayer;
using Content.Server.Station.Systems;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Configurable;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;
using System.Linq;
using Content.Server.Silicons.Laws;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Server.Player;
using Content.Shared.Roles.Jobs;
using Content.Shared.Mind;
using Robust.Shared.Physics.Components;
using static Content.Shared.Configurable.ConfigurationComponent;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly IConGroupController _groupControllerADT = default!;
    [Dependency] private readonly IConsoleHost _consoleADT = default!;
    [Dependency] private readonly IAdminManager _adminManagerADT = default!;
    [Dependency] private readonly IGameTiming _gameTimingADT = default!;
    [Dependency] private readonly IMapManager _mapManagerADT = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManagerADT = default!;
    [Dependency] private readonly AdminSystem _adminSystemADT = default!;
    [Dependency] private readonly DisposalTubeSystem _disposalTubesADT = default!;
    [Dependency] private readonly EuiManager _euiADTManagerADT = default!;
    [Dependency] private readonly GameTicker _tickerADT = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRoleSystemADT = default!;
    [Dependency] private readonly ArtifactSystem _artifactSystemADT = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystemADT = default!;
    [Dependency] private readonly PrayerSystem _prayerSystemADT = default!;
    [Dependency] private readonly EuiManager _euiADT = default!;
    [Dependency] private readonly MindSystem _mindSystemADT = default!;
    [Dependency] private readonly ToolshedManager _toolshedADT = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenateADT = default!;
    [Dependency] private readonly SharedPopupSystem _popupADT = default!;
    [Dependency] private readonly StationSystem _stationsADT = default!;
    [Dependency] private readonly StationSpawningSystem _spawningADT = default!;
    [Dependency] private readonly ExamineSystemShared _examineADT = default!;
    [Dependency] private readonly AdminFrozenSystem _freezeADT = default!;
    [Dependency] private readonly IPlayerManager _playerManagerADT = default!;
    [Dependency] private readonly SiliconLawSystem _siliconLawSystemADT = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;


    // All antag verbs have names so invokeverb works.
    private void AddAdminTimeOperSpawnVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (_adminManagerADT.IsAdmin(player))
        {
            if (TryComp(args.Target, out ActorComponent? targetActor))
            {
                // Spawn - Like respawn but on the spot.
                args.Verbs.Add(new Verb()
                {
                    //Text = Loc.GetString("admin-player-actions-spawn"),
                    Text = "admin-player-actions-spawn",
                    Category = VerbCategory.Admin,
                    Icon = new SpriteSpecifier.Rsi(new("/Textures/ADT/Interface/Misc/time_patrol.rsi"), "icon"),
                    Act = () =>
                    {
                        if (!_transformSystem.TryGetMapOrGridCoordinates(args.Target, out var coords))
                        {
                            _popupADT.PopupEntity("admin-player-spawn-failed", args.User, args.User);
                            return;
                        }

                        var stationUid = _stationsADT.GetOwningStation(args.Target);
                        string? jobId = "ADTJobTimePatrol";
                        var jobPrototype = _prototypeManager.Index<JobPrototype>(jobId);
                        var job = new JobComponent {Prototype = jobId};
                        var profile = _tickerADT.GetPlayerProfile(targetActor.PlayerSession);
                        var mobUid = _spawningADT.SpawnPlayerMob(coords.Value, job, profile, stationUid);
                        var targetMind = _mindSystemADT.GetMind(args.Target);

                        if (TryComp(mobUid, out TransformComponent? transform))
                        {
                            var coordinates = _transform.GetMoverCoordinates(mobUid, transform);
                            var filter = Filter.Pvs(coordinates, 1, EntityManager, _playerManager);
                            var audioParams = new AudioParams().WithVolume(3);
                            _audio.PlayStatic("/Audio/Magic/forcewall.ogg", filter, coordinates, true, audioParams);
                        }


                        if (targetMind != null)
                        {
                            _mindSystemADT.TransferTo(targetMind.Value, mobUid, true);
                        }
                    },
                    ConfirmationPopup = true,
                    Impact = LogImpact.High,
                });
            }
        }
    }
}
