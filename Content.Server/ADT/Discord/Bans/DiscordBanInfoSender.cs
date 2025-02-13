using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Discord;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.ADT.CCVar;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Discord.Bans;

public sealed class DiscordBanInfoSender : IDiscordBanInfoSender
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;

    public async Task SendBanInfoAsync<TGenerator>(BanInfo info)
        where TGenerator : IDiscordBanPayloadGenerator, new()
    {
        var webhookUrl = _cfg.GetCVar(ADTCCVars.DiscordBansWebhook);

        if (string.IsNullOrEmpty(webhookUrl))
            return;

        if (await _discord.GetWebhook(webhookUrl) is not { } webhookData)
            return;

        AddAdditionalInfo(info);
        LocalizeAdditionalInfo(info);

        var identifier = webhookData.ToIdentifier();

        var payload = new TGenerator().Generate(info);

        await _discord.CreateMessage(identifier, payload);
    }

    private void AddAdditionalInfo(BanInfo info)
    {
        var gameTicker = _entitySystemManager.GetEntitySystem<GameTicker>();

        info.AdditionalInfo["serverName"] = _cfg.GetCVar(CCVars.GameHostName);
        info.AdditionalInfo["round"] = gameTicker.RunLevel switch
        {
            GameRunLevel.PreRoundLobby => gameTicker.RoundId == 0
                ? "pre-round lobby after server restart"
                : $"pre-round lobby for round {gameTicker.RoundId + 1}",
            GameRunLevel.InRound => $"round {gameTicker.RoundId}",
            GameRunLevel.PostRound => $"post-round {gameTicker.RoundId}",
            _ => throw new ArgumentOutOfRangeException(nameof(gameTicker.RunLevel),
                $"{gameTicker.RunLevel} was not matched."),
        };
    }

    private void LocalizeAdditionalInfo(BanInfo info)
    {
        LocalizeRole(info);
        LocalizeDepartment(info);
        LocalizeBanPanelData(info);
    }

    private void LocalizeRole(BanInfo info)
    {
        info.AdditionalInfo["localizedRole"] = string.Empty;

        if (info.AdditionalInfo.ContainsKey("role"))
        {
            var jobFound = _protoManager.TryIndex<JobPrototype>(info.AdditionalInfo["role"], out var jobProto);
            info.AdditionalInfo["localizedRole"] = jobFound ? jobProto!.LocalizedName : info.AdditionalInfo["role"];
        }
    }

    private void LocalizeDepartment(BanInfo info)
    {
        info.AdditionalInfo["localizedDepartment"] = string.Empty;

        if (info.AdditionalInfo.ContainsKey("department"))
        {
            var departmentFound = _protoManager
                .TryIndex<DepartmentPrototype>(info.AdditionalInfo["department"],
                out var departmentProto);

            info.AdditionalInfo["localizedDepartment"] = departmentFound
                ? Loc.GetString($"department-{departmentProto!.ID}")
                : info.AdditionalInfo["department"];
        }
    }

    //Не трогай, а то убьёт
    private void LocalizeBanPanelData(BanInfo info)
    {
        info.AdditionalInfo["localizedPanelData"] = string.Empty;

        if (info.AdditionalInfo.ContainsKey("roles"))
        {
            var bannedRolesAndDepartments = new List<string>();

            var roles = info.AdditionalInfo["roles"]
                .Split(", ")
                .Select(x => new
                {
                    Role = x.Split(':')[0],
                    BanId = x.Split(':')[1]
                });

            var rolesPrototypes = _protoManager.EnumeratePrototypes<JobPrototype>();
            var departmentPrototypes = _protoManager.EnumeratePrototypes<DepartmentPrototype>();

            var applicableRolesPrototypes = rolesPrototypes.Where(x => roles.Select(y => y.Role).Contains(x.ID));
            var applicableRolesProtoIds = applicableRolesPrototypes.Select(x => x.ID);

            var applicableDepartmentPrototypes = departmentPrototypes
            .Where(dep => dep.Roles.All(roleProtoId => applicableRolesProtoIds.Contains(roleProtoId.ToString())))
            .Select(dep => new
            {
                DepartmentProto = dep,
                BanIds = roles.Where(roleData => dep.Roles.Select(role => role.Id.ToString())
                .Contains(roleData.Role)).Select(x => x.BanId)
            });

            var rolesPrototypesWithBanIds = applicableRolesPrototypes.Where(role => !applicableDepartmentPrototypes
            .SelectMany(dep => dep.DepartmentProto.Roles.Select(z => z.Id)).Contains(role.ID))
            .Select(roleProto => new
            {
                Role = roleProto,
                BanId = roles.FirstOrDefault(roleData => roleData.Role == roleProto.ID)!.BanId
            });

            var localizedDepartments = applicableDepartmentPrototypes
            .Select(x => new
            {
                LocalizedDepName = Loc.GetString($"department-{x.DepartmentProto.ID}"),
                BanIds = x.BanIds
            })
            .Select(x => Loc.GetString("discord-ban-panel-ban-department-wrapper",
                ("department", x.LocalizedDepName),
                ("banIds", string.Join(", ", x.BanIds))));

            var localizedRoles = rolesPrototypesWithBanIds
            .Select(x => Loc.GetString("discord-ban-panel-ban-role-wrapper",
                ("role", x.Role.LocalizedName),
                ("banId", x.BanId)));

            bannedRolesAndDepartments.AddRange(localizedDepartments);
            bannedRolesAndDepartments.AddRange(localizedRoles);

            info.AdditionalInfo["localizedPanelData"] = string.Join(", ", bannedRolesAndDepartments);
        }
    }
}
