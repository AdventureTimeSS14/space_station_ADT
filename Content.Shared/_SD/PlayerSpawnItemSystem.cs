// using Robust.Shared.Configuration;
// using Robust.Shared.Utility;
// using Robust.Shared.Player;
// using YamlDotNet.Serialization;
// using YamlDotNet.Serialization.NamingConventions;
// using System.Linq;
// using Content.Shared.GameTicking;

// // namespace Content.Shared.Prayer;

// public sealed class PlayerSpawnItemSystem : EntitySystem
// {
//     [Dependency] private readonly IConfigurationManager _cfg = default!;
//     [Dependency] private readonly IPlayerManager _playerManager = default!;
//     [Dependency] private readonly IEntityManager _entityManager = default!;

//     private Dictionary<string, List<string>> _cidRewards = new();

//     public override void Initialize()
//     {
//         base.Initialize();
//         LoadSpecialPlayersConfig();
//         SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
//     }

//     private void LoadSpecialPlayersConfig()
//     {
//         var path = "/Secrets/Players.yml";
//         var resMan = IoCManager.Resolve<IResourceManager>();

//         if (!resMan.TryContentFileRead(path, out var stream))
//             return;

//         using var reader = new StreamReader(stream);
//         var deserializer = new DeserializerBuilder()
//             .WithNamingConvention(UnderscoredNamingConvention.Instance)
//             .Build();

//         var config = deserializer.Deserialize<SpecialPlayersConfig>(reader);
//         _cidRewards = config.SpecialPlayers.ToDictionary(x => x.Cid, x => x.Items);
//     }

//     private void OnPlayerSpawned(PlayerSpawnCompleteEvent args)
//     {
//         if (!_playerManager.TryGetSessionByEntity(args.Mob, out var session))
//             return;

//         var cid = session.ConnectedClient.UserId.ToString();

//         if (!_cidRewards.TryGetValue(cid, out var items))
//             return;

//         var coords = _entityManager.GetComponent<TransformComponent>(args.Mob).Coordinates;

//         foreach (var itemId in items)
//         {
//             var item = _entityManager.SpawnEntity(itemId, coords);

//             // Если нужно положить в руки
//             if (_entityManager.TryGetComponent<HandsComponent>(args.Mob, out var hands))
//             {
//                 _entityManager.System<HandsSystem>().TryPickup(args.Mob, item, hands.ActiveHand);
//             }
//         }
//     }
// }

// public sealed class SpecialPlayersConfig
// {
//     public List<SpecialPlayerEntry> SpecialPlayers { get; set; } = new();
// }

// public sealed class SpecialPlayerEntry
// {
//     public string Cid { get; set; } = "";
//     public List<string> Items { get; set; } = new();
// }
