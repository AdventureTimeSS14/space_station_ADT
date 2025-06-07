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

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    private void AddAdminGodrSpawnVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actorcomponent))
            return;

        var playersession = actorcomponent.PlayerSession;

        if (!_adminManager.HasAdminFlag(playersession, AdminFlags.Admin))
            return;

        if (_adminManager.IsAdmin(playersession))
        {
            if (TryComp(args.Target, out ActorComponent? targetActor))
            {
                args.Verbs.Add(new Verb()
                {
                    Text = Loc.GetString("admin-player-actions-god-shape"),
                    Category = VerbCategory.Admin,
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
                    Act = () =>
                    {
                        if (!_transformSystem.TryGetMapOrGridCoordinates(args.Target, out var coords))
                        {
                            _popup.PopupEntity(Loc.GetString("admin-player-spawn-failed"), args.User, args.User);
                            return;
                        }

                        var targetMind = _mindSystem.GetMind(args.Target);

                        // Спавнит прототип
                        var prototypeId = "ADTGodSpawn";
                        var spawnedEntity = EntityManager.SpawnEntity(prototypeId, coords.Value);

                        // Проигрывает звук при спавне
                        _audio.PlayPvs("/Audio/Effects/holy.ogg", spawnedEntity);

                        EnsureComp<ElectrionPulseActComponent>(spawnedEntity);

                        // Вселяет args.Target в прототип
                        if (targetMind != null)
                        {
                            _mindSystem.TransferTo(targetMind.Value, spawnedEntity, true);
                        }
                    },
                    ConfirmationPopup = true,
                    Impact = LogImpact.High,
                });
            }
        }
    }
}
