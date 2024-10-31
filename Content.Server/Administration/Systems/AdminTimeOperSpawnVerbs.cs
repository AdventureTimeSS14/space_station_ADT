using Content.Shared.Database;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Audio.Systems;
using Content.Shared.ComponentalActions.Components;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Content.Server.Administration.Managers;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IAdminManager _admins = default!;


    // All antag verbs have names so invokeverb works.
    private void AddAdminTimeOperSpawnVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;
        //var hasBan = _admins.HasAdminFlag(player, AdminFlags.Ban);
        if (_adminManager.IsAdmin(player))
        {
            if (TryComp(args.Target, out ActorComponent? targetActor))
            {
                // Spawn Time Patrol
                args.Verbs.Add(new Verb()
                {
                    Text = Loc.GetString("admin-player-actions-time-patrol"),
                    Category = VerbCategory.Admin,
                    Icon = new SpriteSpecifier.Rsi(new("/Textures/ADT/Interface/Misc/time_patrol.rsi"), "icon"),
                    Act = () =>
                    {
                        if (!_transformSystem.TryGetMapOrGridCoordinates(args.Target, out var coords))
                        {
                            _popup.PopupEntity(Loc.GetString("admin-player-spawn-failed"), args.User, args.User);
                            return;
                        }

                        var stationUid = _stations.GetOwningStation(args.Target);
                        string? jobId = "ADTJobTimePatrol";
                        var jobPrototype = _prototypeManager.Index<JobPrototype>(jobId);
                        var job = new JobPrototype {ID = jobId};
                        var profile = _ticker.GetPlayerProfile(targetActor.PlayerSession);
                        var mobUid = _spawning.SpawnPlayerMob(coords.Value, job, profile, stationUid);
                        var targetMind = _mindSystem.GetMind(args.Target);
                        _audio.PlayPvs("/Audio/Magic/forcewall.ogg", mobUid);

                        EnsureComp<TeleportActComponent>(mobUid, out var teleport);
                        if (teleport?.ActionEntity != null)
                        {
                            EnsureComp<WorldTargetActionComponent>(teleport.ActionEntity.Value).UseDelay = TimeSpan.FromSeconds(1);
                        }
                        EnsureComp<ElectrionPulseActComponent>(mobUid);

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
