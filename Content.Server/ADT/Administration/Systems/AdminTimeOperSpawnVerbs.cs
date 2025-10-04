using Content.Server.GameTicking;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Content.Shared.ComponentalActions.Components;
using Content.Shared.Database;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Actions.Components;


// ADT Content: Time Patrol "ОБВА" by 🐾 Schrödinger's Code 🐾
/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/
namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private void AddAdminTimeOperSpawnVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            return;

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
                        ProtoId<JobPrototype> job = "ADTJobTimePatrol";
                        var profile = _gameTicker.GetPlayerProfile(targetActor.PlayerSession);
                        var mobUid = _spawning.SpawnPlayerMob(coords.Value, job, profile, stationUid);
                        var targetMind = _mindSystem.GetMind(args.Target);
                        _audio.PlayPvs("/Audio/Magic/forcewall.ogg", mobUid);

                        EnsureComp<TeleportActComponent>(mobUid, out var teleport);
                        if (teleport?.ActionEntity != null)
                        {
                            // EnsureComp<ActionComponent>(teleport.ActionEntity.Value).UseDelay = TimeSpan.FromSeconds(1); RAT tweak, сейчас починить не могу, починить потом
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
