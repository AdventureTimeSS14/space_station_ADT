using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.ADT.Sponsors;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Shared.Network;

namespace Content.Server.ADT.Sponsors;

public sealed class SponsorPanelEui : BaseEui
{
    [Dependency] private readonly SponsorManager _sponsors = default!;
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private readonly ISawmill _sawmill;

    private NetUserId? _target;
    private string _targetName = string.Empty;
    private SponsorGrant[] _grants = Array.Empty<SponsorGrant>();
    private string _status = string.Empty;

    public SponsorPanelEui()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _log.GetSawmill("sponsors.adt.eui");
    }

    public override void Opened()
    {
        base.Opened();

        _admins.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();

        _admins.OnPermsChanged -= OnPermsChanged;
    }

    public override EuiStateBase GetNewState()
    {
        var state = new SponsorPanelEuiState
        {
            HasPermission = HasPermission(),
            Tiers = _sponsors.Tiers.OrderBy(t => t.Priority).ThenBy(t => t.Id).ToArray(),
            PlayerId = _target?.UserId,
            PlayerName = _targetName,
            Grants = _grants,
            Status = _status,
        };

        if (_target != null && _sponsors.TryGetData(_target.Value, out var data))
            state.Resolved = BuildResolvedView(data);

        return state;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is CloseEuiMessage)
            return;

        if (!HasPermission())
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId}) попытался войти в меню спонсоров без права Host.");
            Report("Нет прав.");
            return;
        }

        switch (msg)
        {
            case SponsorPanelEuiMsg.LookupPlayer m:
                LookupPlayer(m.Query);
                break;

            case SponsorPanelEuiMsg.SaveTier m:
                SaveTier(m.Tier);
                break;

            case SponsorPanelEuiMsg.DeleteTier m:
                DeleteTier(m.TierId);
                break;

            case SponsorPanelEuiMsg.SaveGrant m:
                SaveGrant(m.Grant);
                break;

            case SponsorPanelEuiMsg.RevokeGrant m:
                RevokeGrant(m.GrantId);
                break;
        }
    }

    private async void LookupPlayer(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return;

        try
        {
            var located = await _locator.LookupIdByNameOrIdAsync(query.Trim());

            if (located == null)
            {
                _target = null;
                _targetName = string.Empty;
                _grants = Array.Empty<SponsorGrant>();
                Report($"Игрок '{query}' не найден.");
                return;
            }

            _target = located.UserId;
            _targetName = located.Username;

            await ReloadGrants();
            Report($"Найден {located.Username}.");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Поиск игрока '{query}' сорвался: {ex}");
            Report("Не удалось найти игрока, подробности в логе сервера.");
        }
    }

    private async void SaveTier(SponsorTier tier)
    {
        try
        {
            if (tier.Id == 0)
            {
                var created = await _sponsors.CreateTierAsync(tier, ActorName());
                Report(created == null
                    ? $"Имя '{tier.Name}' уже занято."
                    : $"Тир '{created.Name}' создан.");
                return;
            }

            var updated = await _sponsors.UpdateTierAsync(tier, ActorName());
            Report(updated
                ? $"Тир '{tier.Name}' сохранён."
                : "Не удалось сохранить тир.");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Сохранение тира сорвалось: {ex}");
            Report("Не удалось сохранить тир, подробности в логе сервера.");
        }
    }

    private async void DeleteTier(int tierId)
    {
        try
        {
            var deleted = await _sponsors.DeleteTierAsync(tierId, ActorName());
            Report(deleted
                ? "Тир удалён."
                : "Тир не найден.");

            await ReloadGrants();
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Удаление тира {tierId} сорвалось: {ex}");
            Report("Не удалось удалить тир, подробности в логе сервера.");
        }
    }

    private async void SaveGrant(SponsorGrant grant)
    {
        if (_target == null)
        {
            Report("Сперва найдите игрока.");
            return;
        }

        try
        {
            grant.UserId = _target.Value.UserId;

            if (grant.Id == 0)
            {
                grant.CreatedBy = Player.UserId.UserId;

                var created = await _sponsors.AddGrantAsync(grant, ActorName());
                Report(created == null
                    ? "Выдача должна ссылаться на тир либо нести персональную надстройку."
                    : $"Выдача {created.Id} создана.");
            }
            else
            {
                var updated = await _sponsors.UpdateGrantAsync(grant, ActorName());
                Report(updated ? $"Выдача {grant.Id} сохранена." : "Не удалось сохранить выдачу.");
            }

            await ReloadGrants();
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Сохранение выдачи сорвалось: {ex}");
            Report("Не удалось сохранить выдачу, подробности в логе сервера.");
        }
    }

    private async void RevokeGrant(int grantId)
    {
        try
        {
            var revoked = await _sponsors.RevokeGrantAsync(grantId, Player.UserId.UserId, ActorName());
            Report(revoked ? $"Выдача {grantId} отозвана." : "Выдача не найдена либо уже отозвана.");

            await ReloadGrants();
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Отзыв выдачи {grantId} сорвался: {ex}");
            Report("Не удалось отозвать выдачу, подробности в логе сервера.");
        }
    }

    private bool HasPermission()
    {
        return _admins.HasAdminFlag(Player, AdminFlags.Host);
    }

    private string ActorName()
    {
        return $"{Player.Name} ({Player.UserId})";
    }

    private async Task ReloadGrants()
    {
        if (_target == null)
        {
            _grants = Array.Empty<SponsorGrant>();
            return;
        }

        var grants = await _sponsors.GetGrantHistoryAsync(_target.Value.UserId);
        _grants = grants.ToArray();
        StateDirty();
    }

    private void Report(string status)
    {
        _status = status;
        StateDirty();
    }

    private static SponsorBenefits BuildResolvedView(SponsorData data)
    {
        var benefits = new SponsorBenefits
        {
            RoleBypass = data.RoleBypass,
            AllLoadouts = data.AllLoadouts,
            AllMarkings = data.AllMarkings,
            OocColor = data.OocColor,
            AllowCustomOocColor = data.AllowCustomOocColor,
            GhostColors = data.GhostColors.ToList(),
            AllowCustomGhostColor = data.AllowCustomGhostColor,
            PriorityJoin = data.PriorityJoin,
            ExtraCharacterSlots = data.ExtraCharacterSlots,
        };

        benefits.ExcludedDepartments.UnionWith(data.ExcludedDepartments);
        benefits.ExcludedJobs.UnionWith(data.ExcludedJobs);
        benefits.Loadouts.UnionWith(data.Loadouts);
        benefits.Markings.UnionWith(data.Markings);
        benefits.Species.UnionWith(data.Species);
        benefits.Traits.UnionWith(data.Traits);
        benefits.DiscordRoles.UnionWith(data.DiscordRoles);

        return benefits;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player)
            StateDirty();
    }
}
