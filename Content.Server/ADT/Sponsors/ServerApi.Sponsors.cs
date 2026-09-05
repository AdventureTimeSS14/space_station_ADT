using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Server.ADT.Sponsors;
using Content.Shared.ADT.Sponsors;
using Robust.Server.ServerStatus;

namespace Content.Server.Administration;

public sealed partial class ServerApi
{
    [Dependency] private readonly SponsorManager _sponsors = default!;

    private void RegisterSponsorHandlers()
    {
        RegisterSponsorHandler(HttpMethod.Get, "/admin/sponsors/tiers", GetSponsorTiers);
        RegisterSponsorHandler(HttpMethod.Get, "/admin/sponsors/player", GetSponsorPlayer);
        RegisterSponsorHandler(HttpMethod.Get, "/admin/sponsors/discord_roles", GetSponsorDiscordRoles);

        RegisterSponsorActorHandler(HttpMethod.Post, "/admin/sponsors/tiers/create", CreateSponsorTier);
        RegisterSponsorActorHandler(HttpMethod.Post, "/admin/sponsors/tiers/update", UpdateSponsorTier);
        RegisterSponsorActorHandler(HttpMethod.Post, "/admin/sponsors/tiers/delete", DeleteSponsorTier);

        RegisterSponsorActorHandler(HttpMethod.Post, "/admin/sponsors/grants/create", CreateSponsorGrant);
        RegisterSponsorActorHandler(HttpMethod.Post, "/admin/sponsors/grants/update", UpdateSponsorGrant);
        RegisterSponsorActorHandler(HttpMethod.Post, "/admin/sponsors/grants/revoke", RevokeSponsorGrant);
    }

    private void RegisterSponsorHandler(HttpMethod method, string exactPath, Func<IStatusHandlerContext, Task> handler)
    {
        _statusHost.AddHandler(async context =>
        {
            if (context.RequestMethod != method || context.Url.AbsolutePath != exactPath)
                return false;

            if (!await CheckSponsorAccess(context))
                return true;

            try
            {
                await handler(context);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Ошибка в обработчике спонсорского API {exactPath}: {e}");

                try
                {
                    await RespondError(
                        context,
                        ErrorCode.None,
                        HttpStatusCode.InternalServerError,
                        "Внутренняя ошибка сервера.");
                }
                catch (Exception respondError)
                {
                    _sawmill.Debug($"Не удалось отдать ошибку клиенту: {respondError.Message}");
                }
            }

            return true;
        });
    }

    private void RegisterSponsorActorHandler(HttpMethod method, string exactPath, Func<IStatusHandlerContext, Actor, Task> handler)
    {
        RegisterSponsorHandler(method, exactPath, async context =>
        {
            if (await CheckActor(context) is not { } actor)
                return;

            await handler(context, actor);
        });
    }

    private async Task<bool> CheckSponsorAccess(IStatusHandlerContext context)
    {
        if (!context.RequestHeaders.TryGetValue("Authorization", out var authToken))
        {
            await RespondError(
                context,
                ErrorCode.AuthenticationNeeded,
                HttpStatusCode.Unauthorized,
                "Authorization is required");
            return false;
        }

        var authHeaderValue = authToken.ToString();
        var spaceIndex = authHeaderValue.IndexOf(' ');

        if (spaceIndex == -1)
        {
            await RespondBadRequest(context, "Invalid Authorization header value");
            return false;
        }

        if (authHeaderValue[..spaceIndex] != SS14TokenScheme)
        {
            await RespondBadRequest(context, "Invalid Authorization scheme");
            return false;
        }

        var authValue = authHeaderValue[spaceIndex..].Trim();
        var expected = _config.GetCVar(SponsorCVars.ApiToken);

        if (string.IsNullOrEmpty(expected))
        {
            await RespondError(
                context,
                ErrorCode.AuthenticationInvalid,
                HttpStatusCode.Unauthorized,
                "Sponsor API is disabled");

            return false;
        }

        if (TokenMatches(authValue, expected))
            return true;

        await RespondError(
            context,
            ErrorCode.AuthenticationInvalid,
            HttpStatusCode.Unauthorized,
            "Authorization is invalid");

        _sawmill.Info($"Unauthorized access attempt to sponsor API from {context.RemoteEndPoint}");
        return false;
    }

    private static bool TokenMatches(string provided, string expected)
    {
        if (string.IsNullOrEmpty(expected))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expected));
    }

    private async Task GetSponsorTiers(IStatusHandlerContext context)
    {
        var tiers = await RunOnMainThread(() => _sponsors.Tiers.OrderBy(t => t.Priority).ToArray());

        await RespondSponsorJson(context, tiers);
    }

    private async Task GetSponsorDiscordRoles(IStatusHandlerContext context)
    {
        var map = await RunOnMainThreadAsync(() => _sponsors.GetDiscordRoleMapAsync());

        var players = new Dictionary<string, string[]>(map.Players.Count);

        foreach (var (userId, roles) in map.Players)
        {
            players[userId.ToString()] = roles;
        }

        await RespondSponsorJson(context, new SponsorDiscordRolesResponse
        {
            Managed = map.Managed.ToArray(),
            Players = players,
        });
    }

    private async Task GetSponsorPlayer(IStatusHandlerContext context)
    {
        var userId = await ResolveUser(context);

        if (userId == null)
            return;

        var grants = await _sponsors.GetGrantHistoryAsync(userId.Value);

        var resolved = await RunOnMainThread(() =>
        {
            var data = _sponsors.GetData(new Robust.Shared.Network.NetUserId(userId.Value));
            return data.HasAnyBenefit ? BuildResolved(data) : null;
        });

        await RespondSponsorJson(context, new SponsorPlayerResponse
        {
            UserId = userId.Value,
            Grants = grants.ToArray(),
            Resolved = resolved,
        });
    }

    private async Task CreateSponsorTier(IStatusHandlerContext context, Actor actor)
    {
        var body = await ReadSponsorJson<SponsorTierBody>(context);

        if (body == null)
            return;

        if (string.IsNullOrWhiteSpace(body.Name))
        {
            await RespondBadRequest(context, "Поле 'name' обязательно.");
            return;
        }

        var tier = new SponsorTier
        {
            Name = body.Name,
            DisplayName = body.DisplayName ?? body.Name,
            Description = body.Description ?? string.Empty,
            Priority = body.Priority ?? 0,
            Enabled = body.Enabled ?? true,
            Benefits = body.Benefits ?? new SponsorBenefits(),
        };

        var created = await RunOnMainThreadAsync(() => _sponsors.CreateTierAsync(tier, FormatLogActor(actor)));

        if (created == null)
        {
            await RespondError(
                context,
                ErrorCode.BadRequest,
                HttpStatusCode.Conflict,
                $"Тир с именем '{body.Name}' уже существует.");
            return;
        }

        await RespondSponsorJson(context, created);
    }

    private async Task UpdateSponsorTier(IStatusHandlerContext context, Actor actor)
    {
        var body = await ReadSponsorJson<SponsorTierBody>(context);

        if (body == null)
            return;

        var existing = await RunOnMainThread(() => FindTier(body.Id, body.Name));

        if (existing == null)
        {
            await RespondNotFound(context, "Тир не найден.");
            return;
        }

        var tier = existing.Clone();

        if (body.Name != null)
            tier.Name = body.Name;

        if (body.DisplayName != null)
            tier.DisplayName = body.DisplayName;

        if (body.Description != null)
            tier.Description = body.Description;

        if (body.Priority != null)
            tier.Priority = body.Priority.Value;

        if (body.Enabled != null)
            tier.Enabled = body.Enabled.Value;

        if (body.Benefits != null)
            tier.Benefits = body.Benefits;

        if (!await RunOnMainThreadAsync(() => _sponsors.UpdateTierAsync(tier, FormatLogActor(actor))))
        {
            await RespondError(
                context,
                ErrorCode.BadRequest,
                HttpStatusCode.Conflict,
                "Не удалось сохранить тир.");
            return;
        }

        await RespondSponsorJson(context, tier);
    }

    private async Task DeleteSponsorTier(IStatusHandlerContext context, Actor actor)
    {
        var body = await ReadSponsorJson<SponsorTierBody>(context);

        if (body == null)
            return;

        var existing = await RunOnMainThread(() => FindTier(body.Id, body.Name));

        if (existing == null)
        {
            await RespondNotFound(context, "Тир не найден.");
            return;
        }

        if (!await RunOnMainThreadAsync(() => _sponsors.DeleteTierAsync(existing.Id, FormatLogActor(actor))))
        {
            await RespondNotFound(context, "Тир не найден либо уже удалён.");
            return;
        }

        await RespondOk(context);
    }

    private async Task CreateSponsorGrant(IStatusHandlerContext context, Actor actor)
    {
        var body = await ReadSponsorJson<SponsorGrantBody>(context);

        if (body == null)
            return;

        var userId = await ResolveUser(context, body.UserId, body.UserName);

        if (userId == null)
            return;

        int? tierId = null;

        if (body.Tier != null || body.TierId != null)
        {
            var tier = await RunOnMainThread(() => FindTier(body.TierId, body.Tier));

            if (tier == null)
            {
                await RespondNotFound(context, "Тир не найден.");
                return;
            }

            tierId = tier.Id;
        }

        if (!TryResolveExpiry(body, out var expires, out var expiryError))
        {
            await RespondBadRequest(context, expiryError);
            return;
        }

        var grant = new SponsorGrant
        {
            UserId = userId.Value,
            TierId = tierId,
            Priority = body.Priority ?? 0,
            Overrides = body.Overrides,
            Comment = body.Comment ?? string.Empty,
            CreatedBy = actor.Guid,
            ExpiresAt = expires,
        };

        var created = await RunOnMainThreadAsync(() => _sponsors.AddGrantAsync(grant, FormatLogActor(actor)));

        if (created == null)
        {
            await RespondBadRequest(context, "Выдача должна ссылаться на тир либо нести персональную надстройку.");
            return;
        }

        await RespondSponsorJson(context, created);
    }

    private async Task UpdateSponsorGrant(IStatusHandlerContext context, Actor actor)
    {
        var body = await ReadSponsorJson<SponsorGrantBody>(context);

        if (body == null)
            return;

        if (body.Id == null)
        {
            await RespondBadRequest(context, "Поле 'id' обязательно.");
            return;
        }

        var existing = await _sponsors.GetGrantAsync(body.Id.Value);

        if (existing == null)
        {
            await RespondNotFound(context, "Выдача не найдена.");
            return;
        }

        var grant = existing.Clone();

        if (body.Tier != null || body.TierId != null)
        {
            var tier = await RunOnMainThread(() => FindTier(body.TierId, body.Tier));

            if (tier == null)
            {
                await RespondNotFound(context, "Тир не найден.");
                return;
            }

            grant.TierId = tier.Id;
        }

        if (body.Priority != null)
            grant.Priority = body.Priority.Value;

        if (body.Overrides != null)
            grant.Overrides = body.Overrides;

        if (body.Comment != null)
            grant.Comment = body.Comment;

        if (!TryResolveExpiry(body, out var expires, out var expiryError))
        {
            await RespondBadRequest(context, expiryError);
            return;
        }

        if (body.ExpiresAt != null || body.ExpiresInDays != null || body.Permanent == true)
            grant.ExpiresAt = expires;

        if (!await RunOnMainThreadAsync(() => _sponsors.UpdateGrantAsync(grant, FormatLogActor(actor))))
        {
            await RespondBadRequest(context, "Не удалось сохранить выдачу.");
            return;
        }

        await RespondSponsorJson(context, grant);
    }

    private async Task RevokeSponsorGrant(IStatusHandlerContext context, Actor actor)
    {
        var body = await ReadSponsorJson<SponsorGrantBody>(context);

        if (body == null)
            return;

        if (body.Id == null)
        {
            await RespondBadRequest(context, "Поле 'id' обязательно.");
            return;
        }

        var revoked = await RunOnMainThreadAsync(() =>
            _sponsors.RevokeGrantAsync(body.Id.Value, actor.Guid, FormatLogActor(actor)));

        if (!revoked)
        {
            await RespondNotFound(context, "Выдача не найдена либо уже отозвана.");
            return;
        }

        await RespondOk(context);
    }

    private async Task<T> RunOnMainThreadAsync<T>(Func<Task<T>> func)
    {
        var taskCompletionSource = new TaskCompletionSource<T>();

        // ReSharper disable once AsyncVoidLambda
        _taskManager.RunOnMainThread(async () =>
        {
            try
            {
                taskCompletionSource.TrySetResult(await func());
            }
            catch (Exception e)
            {
                taskCompletionSource.TrySetException(e);
            }
        });

        return await taskCompletionSource.Task;
    }

    private SponsorTier? FindTier(int? id, string? name)
    {
        if (id != null && _sponsors.TryGetTier(id.Value, out var byId))
            return byId;

        if (name != null && _sponsors.TryGetTierByName(name, out var byName))
            return byName;

        return null;
    }

    private Task<Guid?> ResolveUser(IStatusHandlerContext context)
    {
        var query = ParseQuery(context.Url.Query);
        query.TryGetValue("userId", out var rawId);
        query.TryGetValue("userName", out var rawName);

        return ResolveUser(context, rawId == null ? null : Guid.TryParse(rawId, out var g) ? g : null, rawName);
    }

    private async Task<Guid?> ResolveUser(IStatusHandlerContext context, Guid? userId, string? userName)
    {
        if (userId != null)
            return userId;

        if (string.IsNullOrWhiteSpace(userName))
        {
            await RespondBadRequest(context, "Нужен 'userId' либо 'userName'.");
            return null;
        }

        var located = await _playerLocator.LookupIdByNameAsync(userName);

        if (located == null)
        {
            await RespondError(context, ErrorCode.PlayerNotFound, HttpStatusCode.NotFound, $"Игрок '{userName}' не найден.");
            return null;
        }

        return located.UserId.UserId;
    }

    private static bool TryResolveExpiry(SponsorGrantBody body, out DateTime? expires, out string error)
    {
        expires = null;
        error = string.Empty;

        var forms = 0;

        if (body.ExpiresAt != null)
            forms++;

        if (body.ExpiresInDays != null)
            forms++;

        if (body.Permanent == true)
            forms++;

        if (forms > 1)
        {
            error = "Срок задаётся одним из полей 'expiresAt', 'expiresInDays' или 'permanent'.";
            return false;
        }

        if (body.ExpiresInDays != null)
        {
            if (body.ExpiresInDays.Value <= 0)
            {
                error = "'expiresInDays' должно быть больше нуля.";
                return false;
            }

            expires = DateTime.UtcNow.AddDays(body.ExpiresInDays.Value);
            return true;
        }

        expires = ToUtc(body.ExpiresAt);
        return true;
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        if (value == null)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        };
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(query))
            return result;

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');

            if (separator <= 0)
                continue;

            var key = Uri.UnescapeDataString(pair[..separator]);
            var value = Uri.UnescapeDataString(pair[(separator + 1)..]);
            result[key] = value;
        }

        return result;
    }

    private static async Task<T?> ReadSponsorJson<T>(IStatusHandlerContext context) where T : class
    {
        try
        {
            var body = await JsonSerializer.DeserializeAsync<T>(context.RequestBody, SponsorSerialization.Options);

            if (body == null)
                await RespondBadRequest(context, "Пустое тело запроса.");

            return body;
        }
        catch (JsonException e)
        {
            await RespondBadRequest(context, "Не удалось разобрать тело запроса.", ExceptionData.FromException(e));
            return null;
        }
    }

    private static async Task RespondSponsorJson(IStatusHandlerContext context, object data)
    {
        var json = JsonSerializer.Serialize(data, SponsorSerialization.Options);
        await context.RespondAsync(json, HttpStatusCode.OK, "application/json");
    }

    private static async Task RespondNotFound(IStatusHandlerContext context, string message)
    {
        await RespondError(context, ErrorCode.BadRequest, HttpStatusCode.NotFound, message);
    }

    private static SponsorResolvedResponse BuildResolved(SponsorData data)
    {
        return new SponsorResolvedResponse
        {
            Tiers = data.Tiers.ToArray(),
            RoleBypass = data.RoleBypass,
            ExcludedDepartments = data.ExcludedDepartments.ToArray(),
            ExcludedJobs = data.ExcludedJobs.ToArray(),
            Loadouts = data.Loadouts.ToArray(),
            AllLoadouts = data.AllLoadouts,
            Markings = data.Markings.ToArray(),
            AllMarkings = data.AllMarkings,
            Species = data.Species.ToArray(),
            Traits = data.Traits.ToArray(),
            OocColor = data.OocColor,
            AllowCustomOocColor = data.AllowCustomOocColor,
            GhostColors = data.GhostColors.ToArray(),
            AllowCustomGhostColor = data.AllowCustomGhostColor,
            DiscordRoles = data.DiscordRoles.ToArray(),
            PriorityJoin = data.PriorityJoin,
            ExtraCharacterSlots = data.ExtraCharacterSlots,
            NextExpiry = data.NextExpiry,
        };
    }

    private sealed class SponsorTierBody
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("priority")]
        public int? Priority { get; set; }

        [JsonPropertyName("enabled")]
        public bool? Enabled { get; set; }

        [JsonPropertyName("benefits")]
        public SponsorBenefits? Benefits { get; set; }
    }

    private sealed class SponsorGrantBody
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("userId")]
        public Guid? UserId { get; set; }

        [JsonPropertyName("userName")]
        public string? UserName { get; set; }

        [JsonPropertyName("tier")]
        public string? Tier { get; set; }

        [JsonPropertyName("tierId")]
        public int? TierId { get; set; }

        [JsonPropertyName("priority")]
        public int? Priority { get; set; }

        [JsonPropertyName("overrides")]
        public SponsorBenefits? Overrides { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("expiresAt")]
        public DateTime? ExpiresAt { get; set; }

        [JsonPropertyName("expiresInDays")]
        public int? ExpiresInDays { get; set; }

        [JsonPropertyName("permanent")]
        public bool? Permanent { get; set; }
    }

    private sealed class SponsorDiscordRolesResponse
    {
        [JsonPropertyName("managed")]
        public string[] Managed { get; set; } = Array.Empty<string>();

        [JsonPropertyName("players")]
        public Dictionary<string, string[]> Players { get; set; } = new();
    }

    private sealed class SponsorPlayerResponse
    {
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }

        [JsonPropertyName("grants")]
        public SponsorGrant[] Grants { get; set; } = Array.Empty<SponsorGrant>();

        [JsonPropertyName("resolved")]
        public SponsorResolvedResponse? Resolved { get; set; }
    }

    private sealed class SponsorResolvedResponse
    {
        [JsonPropertyName("tiers")]
        public SponsorTierSummary[] Tiers { get; set; } = Array.Empty<SponsorTierSummary>();

        [JsonPropertyName("roleBypass")]
        public SponsorRoleBypass RoleBypass { get; set; }

        [JsonPropertyName("excludedDepartments")]
        public string[] ExcludedDepartments { get; set; } = Array.Empty<string>();

        [JsonPropertyName("excludedJobs")]
        public string[] ExcludedJobs { get; set; } = Array.Empty<string>();

        [JsonPropertyName("loadouts")]
        public string[] Loadouts { get; set; } = Array.Empty<string>();

        [JsonPropertyName("allLoadouts")]
        public bool AllLoadouts { get; set; }

        [JsonPropertyName("markings")]
        public string[] Markings { get; set; } = Array.Empty<string>();

        [JsonPropertyName("allMarkings")]
        public bool AllMarkings { get; set; }

        [JsonPropertyName("species")]
        public string[] Species { get; set; } = Array.Empty<string>();

        [JsonPropertyName("traits")]
        public string[] Traits { get; set; } = Array.Empty<string>();

        [JsonPropertyName("oocColor")]
        public Color? OocColor { get; set; }

        [JsonPropertyName("allowCustomOocColor")]
        public bool AllowCustomOocColor { get; set; }

        [JsonPropertyName("ghostColors")]
        public Color[] GhostColors { get; set; } = Array.Empty<Color>();

        [JsonPropertyName("allowCustomGhostColor")]
        public bool AllowCustomGhostColor { get; set; }

        [JsonPropertyName("discordRoles")]
        public string[] DiscordRoles { get; set; } = Array.Empty<string>();

        [JsonPropertyName("priorityJoin")]
        public bool PriorityJoin { get; set; }

        [JsonPropertyName("extraCharacterSlots")]
        public int ExtraCharacterSlots { get; set; }

        [JsonPropertyName("nextExpiry")]
        public DateTime? NextExpiry { get; set; }
    }
}
