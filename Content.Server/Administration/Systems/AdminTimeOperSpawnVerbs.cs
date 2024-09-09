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
using Robust.Server.GameObjects;
using Content.Shared.Roles.Jobs;
using Content.Shared.Mind;
using Robust.Shared.Physics.Components;
using static Content.Shared.Configurable.ConfigurationComponent;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Server.GameObjects;
using Content.Shared.ComponentalActions.Components;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    // [Dependency] private readonly IAdminManager _adminManager = default!;
    // [Dependency] private readonly GameTicker _ticker = default!;
    // [Dependency] private readonly MindSystem _mindSystem = default!;
    // [Dependency] private readonly SharedPopupSystem _popup = default!;
    // [Dependency] private readonly StationSystem _stations = default!;
    // [Dependency] private readonly StationSpawningSystem _spawning = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;


    // All antag verbs have names so invokeverb works.
    private void AddAdminTimeOperSpawnVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (_adminManager.IsAdmin(player))
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
                            _popup.PopupEntity("admin-player-spawn-failed", args.User, args.User);
                            return;
                        }

                        var stationUid = _stations.GetOwningStation(args.Target);
                        string? jobId = "ADTJobTimePatrol";
                        var jobPrototype = _prototypeManager.Index<JobPrototype>(jobId);
                        var job = new JobComponent {Prototype = jobId};
                        var profile = _ticker.GetPlayerProfile(targetActor.PlayerSession);
                        var mobUid = _spawning.SpawnPlayerMob(coords.Value, job, profile, stationUid);
                        var targetMind = _mindSystem.GetMind(args.Target);
                        _audio.PlayPvs("/Audio/Magic/forcewall.ogg", mobUid);


                        AddComp<TeleportActComponent>(mobUid);
                        AddComp<ElectrionPulseActComponent>(mobUid);

                        if (targetMind != null)
                        {
                            _mindSystem.TransferTo(targetMind.Value, mobUid, true);
                        }
                    },
                    ConfirmationPopup = true,
                    Impact = LogImpact.High,
                });
            }
        }
    }
}
