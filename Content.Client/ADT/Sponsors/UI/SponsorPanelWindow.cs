using System.Globalization;
using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Sponsors;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.ADT.Sponsors.UI;

public sealed class SponsorPanelWindow : DefaultWindow
{
    private readonly LineEdit _playerQuery;
    private readonly Button _playerSearch;
    private readonly Label _playerName;
    private readonly ItemList _grantList;
    private readonly Button _grantRevoke;
    private readonly Label _resolvedLabel;

    private readonly OptionButton _grantTier;
    private readonly LineEdit _grantDays;
    private readonly CheckBox _grantPermanent;
    private readonly LineEdit _grantComment;
    private readonly CheckBox _grantHasOverrides;
    private readonly SponsorBenefitsEditor _grantOverrides;
    private readonly Button _grantCreate;

    private readonly ItemList _tierList;
    private readonly LineEdit _tierName;
    private readonly LineEdit _tierDisplayName;
    private readonly LineEdit _tierDescription;
    private readonly LineEdit _tierPriority;
    private readonly CheckBox _tierEnabled;
    private readonly SponsorBenefitsEditor _tierBenefits;
    private readonly Button _tierSave;
    private readonly Button _tierNew;
    private readonly Button _tierDelete;

    private readonly Label _legacyImportLabel;
    private readonly ItemList _legacyList;
    private readonly LineEdit _legacyNumber;
    private readonly OptionButton _legacyTier;
    private readonly Button _legacySave;
    private readonly Button _legacyDelete;
    private readonly Button _legacyRefresh;

    private readonly Label _status;

    private SponsorTier[] _tiers = Array.Empty<SponsorTier>();
    private SponsorGrant[] _grants = Array.Empty<SponsorGrant>();
    private SponsorLegacyTier[] _legacyTiers = Array.Empty<SponsorLegacyTier>();
    private int? _legacyTargetId;

    private bool _hasPermission;

    private int _editingTierId;

    public event Action<string>? PlayerRequested;
    public event Action<SponsorTier>? TierSaved;
    public event Action<int>? TierDeleted;
    public event Action<SponsorGrant>? GrantCreateRequested;
    public event Action<int>? GrantRevokeRequested;
    public event Action<int, int?>? LegacyTierSaved;
    public event Action<int>? LegacyTierDeleted;
    public event Action? LegacyRefreshRequested;

    public SponsorPanelWindow()
    {
        Title = Loc.GetString("adt-sponsor-panel-title");
        MinSize = new Vector2(900, 660);
        SetSize = new Vector2(980, 720);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
        };

        var tabs = new TabContainer
        {
            VerticalExpand = true,
        };

        var players = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Margin = new Thickness(6),
        };

        var searchRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        _playerQuery = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("adt-sponsor-panel-player-placeholder"),
        };

        _playerSearch = new Button
        {
            Text = Loc.GetString("adt-sponsor-panel-find"),
        };

        searchRow.AddChild(_playerQuery);
        searchRow.AddChild(_playerSearch);
        players.AddChild(searchRow);

        _playerName = new Label
        {
            Text = Loc.GetString("adt-sponsor-panel-no-player"),
            StyleClasses = { "LabelHeading" },
        };
        players.AddChild(_playerName);

        var grantsRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            VerticalExpand = true,
        };

        var grantsLeft = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            HorizontalExpand = true,
        };

        _grantList = new ItemList
        {
            VerticalExpand = true,
            SelectMode = ItemList.ItemListSelectMode.Single,
        };
        grantsLeft.AddChild(_grantList);

        _grantRevoke = new Button
        {
            Text = Loc.GetString("adt-sponsor-panel-revoke"),
        };
        grantsLeft.AddChild(_grantRevoke);

        grantsRow.AddChild(grantsLeft);

        _resolvedLabel = new Label
        {
            Text = string.Empty,
            MinWidth = 300,
            VerticalAlignment = VAlignment.Top,
        };

        grantsRow.AddChild(new ScrollContainer
        {
            MinWidth = 320,
            VerticalExpand = true,
            Children = { _resolvedLabel },
        });

        players.AddChild(grantsRow);

        players.AddChild(new Label
        {
            Text = Loc.GetString("adt-sponsor-panel-new-grant"),
            StyleClasses = { "LabelHeading" },
        });

        var grantForm = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        _grantTier = new OptionButton
        {
            MinWidth = 200,
        };

        _grantDays = new LineEdit
        {
            MinWidth = 60,
            Text = "30",
        };

        _grantPermanent = new CheckBox
        {
            Text = Loc.GetString("adt-sponsor-panel-permanent"),
        };

        _grantComment = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("adt-sponsor-panel-comment"),
        };

        _grantCreate = new Button
        {
            Text = Loc.GetString("adt-sponsor-panel-give"),
        };

        grantForm.AddChild(_grantTier);
        grantForm.AddChild(new Label { Text = Loc.GetString("adt-sponsor-panel-days") });
        grantForm.AddChild(_grantDays);
        grantForm.AddChild(_grantPermanent);
        grantForm.AddChild(_grantComment);
        grantForm.AddChild(_grantCreate);
        players.AddChild(grantForm);

        _grantHasOverrides = new CheckBox
        {
            Text = Loc.GetString("adt-sponsor-panel-overrides"),
        };
        players.AddChild(_grantHasOverrides);

        _grantOverrides = new SponsorBenefitsEditor
        {
            Visible = false,
        };

        players.AddChild(new ScrollContainer
        {
            MinHeight = 160,
            Children = { _grantOverrides },
        });

        tabs.AddChild(players);
        tabs.SetTabTitle(0, Loc.GetString("adt-sponsor-panel-tab-players"));

        var tiers = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            Margin = new Thickness(6),
        };

        var tiersLeft = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            MinWidth = 240,
        };

        _tierList = new ItemList
        {
            VerticalExpand = true,
            SelectMode = ItemList.ItemListSelectMode.Single,
        };
        tiersLeft.AddChild(_tierList);

        _tierNew = new Button
        {
            Text = Loc.GetString("adt-sponsor-panel-tier-new"),
        };

        _tierDelete = new Button
        {
            Text = Loc.GetString("adt-sponsor-panel-tier-delete"),
        };

        tiersLeft.AddChild(_tierNew);
        tiersLeft.AddChild(_tierDelete);
        tiers.AddChild(tiersLeft);

        var tiersRight = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 2,
            HorizontalExpand = true,
        };

        _tierName = AddLabeled(tiersRight, Loc.GetString("adt-sponsor-panel-tier-name"), "tier1");
        _tierDisplayName = AddLabeled(tiersRight, Loc.GetString("adt-sponsor-panel-tier-display"), "Тир 1");
        _tierDescription = AddLabeled(tiersRight, Loc.GetString("adt-sponsor-panel-tier-desc"), string.Empty);
        _tierPriority = AddLabeled(tiersRight, Loc.GetString("adt-sponsor-panel-tier-priority"), "10");

        _tierEnabled = new CheckBox
        {
            Text = Loc.GetString("adt-sponsor-panel-tier-enabled"),
            Pressed = true,
        };
        tiersRight.AddChild(_tierEnabled);

        _tierBenefits = new SponsorBenefitsEditor();

        tiersRight.AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            Children = { _tierBenefits },
        });

        _tierSave = new Button
        {
            Text = Loc.GetString("adt-sponsor-panel-tier-save"),
        };
        tiersRight.AddChild(_tierSave);

        tiers.AddChild(tiersRight);

        tabs.AddChild(tiers);
        tabs.SetTabTitle(1, Loc.GetString("adt-sponsor-panel-tab-tiers"));

        var legacy = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Margin = new Thickness(6),
        };

        _legacyImportLabel = new Label
        {
            Text = string.Empty,
        };
        legacy.AddChild(_legacyImportLabel);

        legacy.AddChild(new Label
        {
            Text = Loc.GetString("adt-sponsor-panel-legacy-hint"),
        });

        _legacyList = new ItemList
        {
            VerticalExpand = true,
            SelectMode = ItemList.ItemListSelectMode.Single,
        };
        legacy.AddChild(_legacyList);

        var legacyForm = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        _legacyNumber = new LineEdit
        {
            MinWidth = 80,
            PlaceHolder = "1",
        };

        _legacyTier = new OptionButton
        {
            MinWidth = 220,
        };

        _legacySave = new Button
        {
            Text = Loc.GetString("adt-sponsor-panel-legacy-save"),
        };

        _legacyDelete = new Button
        {
            Text = Loc.GetString("adt-sponsor-panel-legacy-delete"),
        };

        _legacyRefresh = new Button
        {
            Text = Loc.GetString("adt-sponsor-panel-legacy-refresh"),
        };

        legacyForm.AddChild(new Label { Text = Loc.GetString("adt-sponsor-panel-legacy-number") });
        legacyForm.AddChild(_legacyNumber);
        legacyForm.AddChild(new Label { Text = "->" });
        legacyForm.AddChild(_legacyTier);
        legacyForm.AddChild(_legacySave);
        legacyForm.AddChild(_legacyDelete);
        legacyForm.AddChild(new Control { HorizontalExpand = true });
        legacyForm.AddChild(_legacyRefresh);
        legacy.AddChild(legacyForm);

        tabs.AddChild(legacy);
        tabs.SetTabTitle(2, Loc.GetString("adt-sponsor-panel-tab-legacy"));

        root.AddChild(tabs);

        _status = new Label
        {
            Text = string.Empty,
        };
        root.AddChild(_status);

        Contents.AddChild(root);

        WireEvents();
    }

    private void WireEvents()
    {
        _playerSearch.OnPressed += _ => PlayerRequested?.Invoke(_playerQuery.Text);
        _playerQuery.OnTextEntered += _ => PlayerRequested?.Invoke(_playerQuery.Text);

        _grantHasOverrides.OnToggled += args => _grantOverrides.Visible = args.Pressed;

        _grantTier.OnItemSelected += args => _grantTier.SelectId(args.Id);

        _grantCreate.OnPressed += _ => GrantCreateRequested?.Invoke(BuildGrant());

        _grantRevoke.OnPressed += _ =>
        {
            if (TryGetSelected(_grantList, _grants, out var grant) && !grant.Legacy)
                GrantRevokeRequested?.Invoke(grant.Id);
        };

        _grantList.OnItemSelected += _ => UpdateRevokeButton();
        _grantList.OnItemDeselected += _ => UpdateRevokeButton();

        _legacyTier.OnItemSelected += args =>
        {
            _legacyTier.SelectId(args.Id);
            _legacyTargetId = GetSelectedLegacyTarget();
        };

        _legacyList.OnItemSelected += _ =>
        {
            if (TryGetSelected(_legacyList, _legacyTiers, out var legacyTier))
                LoadLegacyTier(legacyTier);
        };

        _legacySave.OnPressed += _ =>
        {
            if (!int.TryParse(_legacyNumber.Text.Trim(), out var number))
            {
                SetStatus(Loc.GetString("adt-sponsor-panel-legacy-bad-number"));
                return;
            }

            LegacyTierSaved?.Invoke(number, GetSelectedLegacyTarget());
        };

        _legacyDelete.OnPressed += _ =>
        {
            if (int.TryParse(_legacyNumber.Text.Trim(), out var number))
                LegacyTierDeleted?.Invoke(number);
        };

        _legacyRefresh.OnPressed += _ => LegacyRefreshRequested?.Invoke();

        _tierList.OnItemSelected += _ =>
        {
            if (TryGetSelected(_tierList, _tiers, out var tier))
                LoadTier(tier);
        };

        _tierNew.OnPressed += _ => LoadTier(null);

        _tierSave.OnPressed += _ => TierSaved?.Invoke(BuildTier());

        _tierDelete.OnPressed += _ =>
        {
            if (TryGetSelected(_tierList, _tiers, out var tier))
                TierDeleted?.Invoke(tier.Id);
        };
    }

    public void SetPermission(bool allowed)
    {
        _hasPermission = allowed;

        _grantCreate.Disabled = !allowed;
        _tierSave.Disabled = !allowed;
        _tierDelete.Disabled = !allowed;
        _tierNew.Disabled = !allowed;
        _legacySave.Disabled = !allowed;
        _legacyDelete.Disabled = !allowed;

        UpdateRevokeButton();
    }

    private void UpdateRevokeButton()
    {
        if (!_hasPermission)
        {
            _grantRevoke.Disabled = true;
            return;
        }

        _grantRevoke.Disabled = TryGetSelected(_grantList, _grants, out var grant) && grant.Legacy;
    }

    public void SetLegacy(SponsorLegacyTier[] legacyTiers, bool importEnabled)
    {
        _legacyTiers = legacyTiers;

        _legacyImportLabel.Text = importEnabled
            ? Loc.GetString("adt-sponsor-panel-legacy-import-on")
            : Loc.GetString("adt-sponsor-panel-legacy-import-off");

        _legacyTier.Clear();
        _legacyTier.AddItem(Loc.GetString("adt-sponsor-panel-legacy-unmapped"), 0);

        for (var i = 0; i < _tiers.Length; i++)
        {
            _legacyTier.AddItem($"{_tiers[i].Name} - {_tiers[i].DisplayName}", i + 1);
        }

        SelectLegacyTarget(_legacyTargetId);

        _legacyList.Clear();

        foreach (var legacyTier in legacyTiers)
        {
            var target = Loc.GetString("adt-sponsor-panel-legacy-unmapped");

            if (legacyTier.TierId != null)
            {
                var tier = _tiers.FirstOrDefault(t => t.Id == legacyTier.TierId.Value);
                target = tier == null
                    ? Loc.GetString("adt-sponsor-panel-legacy-missing", ("id", legacyTier.TierId.Value))
                    : $"{tier.Name} - {tier.DisplayName}";
            }

            var seen = legacyTier.LastSeen?.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
                ?? Loc.GetString("adt-sponsor-panel-legacy-never");

            _legacyList.AddItem(Loc.GetString("adt-sponsor-panel-legacy-row",
                ("legacy", legacyTier.LegacyTier),
                ("target", target),
                ("online", legacyTier.OnlinePlayers),
                ("seen", seen)));
        }
    }

    private void LoadLegacyTier(SponsorLegacyTier legacyTier)
    {
        _legacyNumber.Text = legacyTier.LegacyTier.ToString(CultureInfo.InvariantCulture);
        SelectLegacyTarget(legacyTier.TierId);
    }

    private void SelectLegacyTarget(int? tierId)
    {
        _legacyTier.SelectId(0);
        _legacyTargetId = null;

        if (tierId == null)
            return;

        for (var i = 0; i < _tiers.Length; i++)
        {
            if (_tiers[i].Id != tierId.Value)
                continue;

            _legacyTier.SelectId(i + 1);
            _legacyTargetId = tierId;
            return;
        }
    }

    private int? GetSelectedLegacyTarget()
    {
        var index = _legacyTier.SelectedId - 1;

        if (index < 0 || index >= _tiers.Length)
            return null;

        return _tiers[index].Id;
    }

    public void SetStatus(string status)
    {
        _status.Text = status;
    }

    public void SetTiers(SponsorTier[] tiers)
    {
        _tiers = tiers;

        _tierList.Clear();

        foreach (var tier in tiers)
        {
            var mark = tier.Enabled ? string.Empty : " (выкл)";
            _tierList.AddItem($"{tier.Name} - {tier.DisplayName}{mark}");
        }

        _grantTier.Clear();
        _grantTier.AddItem(Loc.GetString("adt-sponsor-panel-no-tier"), 0);

        for (var i = 0; i < tiers.Length; i++)
        {
            _grantTier.AddItem(tiers[i].Name, i + 1);
        }

        if (_grantTier.ItemCount > 1)
            _grantTier.SelectId(1);
    }

    public void SetPlayer(string name, SponsorGrant[] grants, SponsorBenefits? resolved)
    {
        _grants = grants;

        _playerName.Text = string.IsNullOrEmpty(name)
            ? Loc.GetString("adt-sponsor-panel-no-player")
            : name;

        _grantList.Clear();

        var now = DateTime.UtcNow;

        foreach (var grant in grants)
        {
            if (grant.Legacy)
            {
                var legacyTier = grant.TierName ?? Loc.GetString("adt-sponsor-panel-no-tier");
                var legacyExpires = grant.ExpiresAt?.ToString("dd.MM.yyyy") ?? Loc.GetString("adt-sponsor-panel-forever");
                var legacyExtra = grant.Overrides == null ? string.Empty : " +";

                _grantList.AddItem(Loc.GetString("adt-sponsor-panel-legacy-grant",
                    ("tier", legacyTier + legacyExtra),
                    ("expires", legacyExpires),
                    ("comment", grant.Comment)));
                continue;
            }

            var state = grant.Revoked
                ? Loc.GetString("adt-sponsor-panel-state-revoked")
                : grant.IsActive(now)
                    ? Loc.GetString("adt-sponsor-panel-state-active")
                    : Loc.GetString("adt-sponsor-panel-state-expired");

            var tier = grant.TierName ?? Loc.GetString("adt-sponsor-panel-no-tier");
            var expires = grant.ExpiresAt?.ToString("dd.MM.yyyy") ?? Loc.GetString("adt-sponsor-panel-forever");
            var extra = grant.Overrides == null ? string.Empty : " +";

            _grantList.AddItem($"[{grant.Id}] {tier}{extra} - {expires} - {state} - {grant.Comment}");
        }

        _resolvedLabel.Text = DescribeResolved(resolved);

        UpdateRevokeButton();
    }

    private SponsorGrant BuildGrant()
    {
        var grant = new SponsorGrant
        {
            Comment = _grantComment.Text,
            Overrides = _grantHasOverrides.Pressed ? _grantOverrides.Build() : null,
        };

        var tierIndex = _grantTier.SelectedId - 1;

        if (tierIndex >= 0 && tierIndex < _tiers.Length)
            grant.TierId = _tiers[tierIndex].Id;

        if (!_grantPermanent.Pressed && int.TryParse(_grantDays.Text, out var days) && days > 0)
            grant.ExpiresAt = DateTime.UtcNow.AddDays(days);

        return grant;
    }

    private SponsorTier BuildTier()
    {
        return new SponsorTier
        {
            Id = _editingTierId,
            Name = _tierName.Text.Trim(),
            DisplayName = _tierDisplayName.Text.Trim(),
            Description = _tierDescription.Text.Trim(),
            Priority = int.TryParse(_tierPriority.Text, out var priority) ? priority : 0,
            Enabled = _tierEnabled.Pressed,
            Benefits = _tierBenefits.Build(),
        };
    }

    private void LoadTier(SponsorTier? tier)
    {
        if (tier == null)
        {
            _editingTierId = 0;
            _tierName.Text = string.Empty;
            _tierDisplayName.Text = string.Empty;
            _tierDescription.Text = string.Empty;
            _tierPriority.Text = "0";
            _tierEnabled.Pressed = true;
            _tierBenefits.Clear();
            return;
        }

        _editingTierId = tier.Id;
        _tierName.Text = tier.Name;
        _tierDisplayName.Text = tier.DisplayName;
        _tierDescription.Text = tier.Description;
        _tierPriority.Text = tier.Priority.ToString(CultureInfo.InvariantCulture);
        _tierEnabled.Pressed = tier.Enabled;
        _tierBenefits.Load(tier.Benefits);
    }

    private static string DescribeResolved(SponsorBenefits? benefits)
    {
        if (benefits == null)
            return Loc.GetString("adt-sponsor-panel-resolved-none");

        var lines = new List<string>
        {
            Loc.GetString("adt-sponsor-panel-resolved-title"),
            string.Empty,
            $"обход ролей: {benefits.RoleBypass}",
        };

        AddList(lines, "кроме департаментов", benefits.ExcludedDepartments);
        AddList(lines, "кроме работ", benefits.ExcludedJobs);
        AddList(lines, "лодауты", benefits.Loadouts);
        AddList(lines, "маркинги", benefits.Markings);
        AddList(lines, "виды", benefits.Species);
        AddList(lines, "трейты", benefits.Traits);
        AddList(lines, "роли дискорда", benefits.DiscordRoles);

        AddFlag(lines, "все спонсорские лодауты", benefits.AllLoadouts);
        AddFlag(lines, "все спонсорские маркинги", benefits.AllMarkings);
        AddFlag(lines, "приоритетный вход", benefits.PriorityJoin);
        AddFlag(lines, "свой цвет ника", benefits.AllowCustomOocColor);
        AddFlag(lines, "свой цвет призрака", benefits.AllowCustomGhostColor);

        if (benefits.OocColor != null)
            lines.Add($"цвет ника: {benefits.OocColor.Value.ToHex()}");

        if (benefits.GhostColors.Count > 0)
            lines.Add($"цвета призрака: {string.Join(", ", benefits.GhostColors.Select(c => c.ToHex()))}");

        if (benefits.ExtraCharacterSlots > 0)
            lines.Add($"доп. слотов: {benefits.ExtraCharacterSlots}");

        return string.Join("\n", lines);
    }

    private static void AddList(List<string> lines, string label, IReadOnlyCollection<string> values)
    {
        if (values.Count > 0)
            lines.Add($"{label}: {string.Join(", ", values)}");
    }

    private static void AddFlag(List<string> lines, string label, bool value)
    {
        if (value)
            lines.Add(label);
    }

    private static LineEdit AddLabeled(Control parent, string label, string placeholder)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        row.AddChild(new Label
        {
            Text = label,
            MinWidth = 220,
        });

        var edit = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = placeholder,
        };

        row.AddChild(edit);
        parent.AddChild(row);

        return edit;
    }

    private static bool TryGetSelected<T>(ItemList list, T[] items, out T item)
    {
        item = default!;

        var selected = list.GetSelected().FirstOrDefault();

        if (selected == null)
            return false;

        var index = list.IndexOf(selected);

        if (index < 0 || index >= items.Length)
            return false;

        item = items[index];
        return true;
    }
}
