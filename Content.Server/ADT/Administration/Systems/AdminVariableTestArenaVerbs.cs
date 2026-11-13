using Content.Shared.Administration;
using Content.Shared.ADT.Administration;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;


/*
    ADT Content by 🐾 Schrödinger's Code 🐾
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
    [Dependency] private AdminTestArenaVariableSystem _adminTestArenaVariableSystem = default!;

    private void AdminTestArenaVariableVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            return;

        var arenas = ProtoMan.EnumeratePrototypes<AdminArenaVerbPrototype>().ToList().OrderBy(x => x.Name);

        // Добавляем вербы для каждой арены из прототипа
        foreach (var arena in arenas)
        {
            Verb sendToArena = new()
            {
                Text = arena.Name,
                Category = VerbCategory.AdminRoom,
                Icon = new SpriteSpecifier.Texture(new(arena.IconAltVerb)),

                Act = () =>
                {
                    var (mapUid, gridUid) = _adminTestArenaVariableSystem.AssertArenaLoaded(
                        player,
                        arena.PathMap,
                        arena.ID
                    );
                    _transformSystem.SetCoordinates(args.Target, new EntityCoordinates(gridUid ?? mapUid, Vector2.One));
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString(arena.Description ?? "No description available"),
                Priority = -100000000,
            };

            args.Verbs.Add(sendToArena);
        }
    }
}

