using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Numerics;

/*
    ADT Content by 🐾 Schrödinger's Code 🐾
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝
            ██╗░░███████╗░░░██╗
            █╔╝░░███╔═══╝░░░░█║
            █║══░╚█████╗░░══░█║
            █║░░░░╚══███╗░░░░█║
            ██╗░░███████║░░░██║
            ╚═╝░░╚══════╝░░░╚═╝
        Wiskey Echo Wiskey Lima Alpha Delta
    ▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔
*/


namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly AdminTestArenaVariableSystem _adminTestArenaVariableSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private static readonly List<(string MapPath, string PrefixName, string IconPath)> AdminTestArenas = new()
    {
        ("/Maps/Test/admin_test_arena.yml", "ClassicARoom", "/Textures/Interface/VerbIcons/eject.svg.192dpi.png"),       // index: 0
        ("/Maps/ADTMaps/ARoom/aroom_dev.yml", "DevBox", "/Textures/ADT/Interface/VerbIcons/icons8-dev.png"),             // index: 1
        ("/Maps/ADTMaps/ARoom/aroom_hell.yml", "Hell", "/Textures/ADT/Interface/VerbIcons/icons8-hell.png"),             // index: 2
        ("/Maps/ADTMaps/ARoom/aroom_paradise.yml", "Paradise", "/Textures/ADT/Interface/VerbIcons/icons8-paradise.png"), // index: 3
        ("/Maps/ADTMaps/ARoom/aroom_sandbox.yml", "SandBox", "/Textures/ADT/Interface/VerbIcons/icons8-sandbox.png"),    // index: 4
    };


    private void AdminTestArenaVariableVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            return;

        // Классическая арума
        Verb sendToTestArenaClassic = new ()
        {
            Text = "Send to test arena classic",
            Category = VerbCategory.AdminRoom,
            Icon = new SpriteSpecifier.Texture(new(AdminTestArenas[0].IconPath)),

            Act = () =>
            {
                var (mapUid, gridUid) = _adminTestArenaVariableSystem.AssertArenaLoaded(
                    player,
                    AdminTestArenas[0].MapPath,
                    AdminTestArenas[0].PrefixName
                );
                _transformSystem.SetCoordinates(args.Target, new EntityCoordinates(gridUid ?? mapUid, Vector2.One));
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-send-to-test-arena-classic-description"),
            Priority = (int) TricksVerbPriorities.Bolt,
        };
        args.Verbs.Add(sendToTestArenaClassic);

        // Арума мини-Dev
        Verb sendToTestArenaDevelop = new()
        {
            Text = "Send to test arena develop",
            Category = VerbCategory.AdminRoom,
            Icon = new SpriteSpecifier.Texture(new(AdminTestArenas[1].IconPath)),

            Act = () =>
            {
                var (mapUid, gridUid) = _adminTestArenaVariableSystem.AssertArenaLoaded(
                    player,
                    AdminTestArenas[1].MapPath,
                    AdminTestArenas[1].PrefixName
                );
                _transformSystem.SetCoordinates(args.Target, new EntityCoordinates(gridUid ?? mapUid, Vector2.One));
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-send-to-test-arena-dev-description"),
            Priority = (int) TricksVerbPriorities.Bolt,
        };
        args.Verbs.Add(sendToTestArenaDevelop);

        // Арума Ад?
        Verb sendToTestArenaHell = new()
        {
            Text = "Send to test arena hell",
            Category = VerbCategory.AdminRoom,
            Icon = new SpriteSpecifier.Texture(new(AdminTestArenas[2].IconPath)),

            Act = () =>
            {
                var (mapUid, gridUid) = _adminTestArenaVariableSystem.AssertArenaLoaded(
                    player,
                    AdminTestArenas[2].MapPath,
                    AdminTestArenas[2].PrefixName
                );
                _transformSystem.SetCoordinates(args.Target, new EntityCoordinates(gridUid ?? mapUid, Vector2.One));
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-send-to-test-arena-hell-description"),
            Priority = (int) TricksVerbPriorities.Bolt,
        };
        args.Verbs.Add(sendToTestArenaHell);

        // Арума Райский уголок
        Verb sendToTestArenaParadise = new()
        {
            Text = "Send to test arena paradise",
            Category = VerbCategory.AdminRoom,
            Icon = new SpriteSpecifier.Texture(new(AdminTestArenas[3].IconPath)),

            Act = () =>
            {
                var (mapUid, gridUid) = _adminTestArenaVariableSystem.AssertArenaLoaded(
                    player,
                    AdminTestArenas[3].MapPath,
                    AdminTestArenas[3].PrefixName
                );
                _transformSystem.SetCoordinates(args.Target, new EntityCoordinates(gridUid ?? mapUid, Vector2.One));
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-send-to-test-arena-paradise-description"),
            Priority = (int) TricksVerbPriorities.Bolt,
        };
        args.Verbs.Add(sendToTestArenaParadise);

        // Просто арума КУБ
        Verb sendToTestArenaSandBox = new()
        {
            Text = "Send to test arena SandBox",
            Category = VerbCategory.AdminRoom,
            Icon = new SpriteSpecifier.Texture(new(AdminTestArenas[4].IconPath)),

            Act = () =>
            {
                var (mapUid, gridUid) = _adminTestArenaVariableSystem.AssertArenaLoaded(
                    player,
                    AdminTestArenas[4].MapPath,
                    AdminTestArenas[4].PrefixName
                );
                _transformSystem.SetCoordinates(args.Target, new EntityCoordinates(gridUid ?? mapUid, Vector2.One));
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-send-to-test-arena-sandbox-description"),
            Priority = (int) TricksVerbPriorities.Bolt,
        };
        args.Verbs.Add(sendToTestArenaSandBox);
    }

}

