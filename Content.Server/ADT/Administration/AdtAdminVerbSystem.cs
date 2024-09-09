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
using Content.Shared.Mind;
using Robust.Shared.Physics.Components;
using static Content.Shared.Configurable.ConfigurationComponent;
using Content.Client.Pointing.Components;
using Content.Shared.Pointing;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Animations;
using System.Numerics;

namespace Content.Server.Administration.Systems
{
    /// <summary>
    ///     System to provide various global admin/debug verbs
    /// </summary>
    public sealed partial class AdtAdminVerbSystem : EntitySystem
    {
        [Dependency] private readonly IConGroupController _groupController = default!;
        [Dependency] private readonly IConsoleHost _console = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly DisposalTubeSystem _disposalTubes = default!;
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly GhostRoleSystem _ghostRoleSystem = default!;
        [Dependency] private readonly ArtifactSystem _artifactSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly PrayerSystem _prayerSystem = default!;
        [Dependency] private readonly EuiManager _eui = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly ToolshedManager _toolshed = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly StationSystem _stations = default!;
        [Dependency] private readonly StationSpawningSystem _spawning = default!;
        [Dependency] private readonly ExamineSystemShared _examine = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly SiliconLawSystem _siliconLawSystem = default!;

        private readonly Dictionary<ICommonSession, List<EditSolutionsEui>> _openSolutionUis = new();

        public override void Initialize()
        {
            SubscribeLocalEvent<GetVerbsEvent<Verb>>(GetVerbsD);
        }

        private void GetVerbsD(GetVerbsEvent<Verb> ev)
        {
            AddAdminVerbsD(ev);
        }

        private void AddAdminVerbsD(GetVerbsEvent<Verb> args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            var player = actor.PlayerSession;

            if (_adminManager.IsAdmin(player))
            {
                Verb mark = new();
                mark.Text = Loc.GetString("toolshed-verb-mark");
                mark.Message = Loc.GetString("toolshed-verb-mark-description");
                mark.Category = VerbCategory.Admin;
                mark.Act = () => _toolshed.InvokeCommand(player, "=> $marked", Enumerable.Repeat(args.Target, 1), out _);
                mark.Impact = LogImpact.Low;
                args.Verbs.Add(mark);

                if (TryComp(args.Target, out ActorComponent? targetActor))
                {
                    // Spawn - Like respawn but on the spot.
                    args.Verbs.Add(new Verb()
                    {
                        Text = Loc.GetString("admin-player-actions-spawn"),
                        Category = VerbCategory.Admin,
                        Act = () =>
                        {
                            if (!_transformSystem.TryGetMapOrGridCoordinates(args.Target, out var coords))
                            {
                                _popup.PopupEntity(Loc.GetString("admin-player-spawn-failed"), args.User, args.User);
                                return;
                            }

                            var stationUid = _stations.GetOwningStation(args.Target);

                            var profile = _ticker.GetPlayerProfile(targetActor.PlayerSession);
                            var mobUid = _spawning.SpawnPlayerMob(coords.Value, null, profile, stationUid);
                            var targetMind = _mindSystem.GetMind(args.Target);

                            if (targetMind != null)
                            {
                                _mindSystem.TransferTo(targetMind.Value, mobUid, true);
                            }
                        },
                        ConfirmationPopup = true,
                        Impact = LogImpact.High,
                    });

                    // Clone - Spawn but without the mind transfer, also spawns at the user's coordinates not the target's
                    args.Verbs.Add(new Verb()
                    {
                        Text = Loc.GetString("admin-player-actions-clone"),
                        Category = VerbCategory.Admin,
                        Act = () =>
                        {
                            if (!_transformSystem.TryGetMapOrGridCoordinates(args.User, out var coords))
                            {
                                _popup.PopupEntity(Loc.GetString("admin-player-spawn-failed"), args.User, args.User);
                                return;
                            }

                            var stationUid = _stations.GetOwningStation(args.Target);

                            var profile = _ticker.GetPlayerProfile(targetActor.PlayerSession);
                            _spawning.SpawnPlayerMob(coords.Value, null, profile, stationUid);
                        },
                        ConfirmationPopup = true,
                        Impact = LogImpact.High,
                    });

                    // PlayerPanel
                    args.Verbs.Add(new Verb
                    {
                        Text = Loc.GetString("admin-player-actions-player-panel"),
                        Category = VerbCategory.Admin,
                        Act = () => _console.ExecuteCommand(player, $"playerpanel \"{targetActor.PlayerSession.UserId}\""),
                        Impact = LogImpact.Low
                    });
                }
            }
        }
    }
}
