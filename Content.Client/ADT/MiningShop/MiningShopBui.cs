using System.Linq;
using Content.Shared.ADT.MiningShop;
using Content.Shared.Mind;
using Content.Shared.ADT.Salvage.Systems;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static System.StringComparison;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Content.Client.ADT.MiningShop;

[UsedImplicitly]
public sealed class MiningShopBui : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    private readonly MiningPointsSystem _miningPoints;
    private MiningShopWindow? _window;
    private List<SharedMiningShopSectionPrototype> _sections = new();
    public MiningShopBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _miningPoints = EntMan.System<MiningPointsSystem>();
    }

    protected override void Open()
    {
        _window = new MiningShopWindow();
        _window.OnClose += Close;
        _window.Title = EntMan.GetComponentOrNull<MetaDataComponent>(Owner)?.EntityName ?? "MiningShop";

        if (!EntMan.TryGetComponent(Owner, out MiningShopComponent? vendor))
            return;
        var sections = _prototype.EnumeratePrototypes<SharedMiningShopSectionPrototype>().ToList();
        sections.Sort((x, y) => x.Name[0].CompareTo(x.Name[0]));

        foreach (var section in sections)
        {
            var uiSection = new MiningShopSection();
            uiSection.Label.SetMessage(GetSectionName(section));
            _sections.Add(section);

            foreach (var entry in section.Entries)
            {
                var uiEntry = new MiningShopEntry();

                if (_prototype.TryIndex(entry.Id, out var entity))
                {
                    uiEntry.Texture.Textures = SpriteComponent.GetPrototypeTextures(entity, _resource)
                        .Select(o => o.Default)
                        .ToList();
                    uiEntry.Panel.Button.Label.Text = entry.Name?.Replace("\\n", "\n") ?? entity.Name;

                    var name = entity.Name;
                    var color = MiningShopPanel.DefaultColor;
                    var borderColor = MiningShopPanel.DefaultBorderColor;
                    var hoverColor = MiningShopPanel.DefaultBorderColor;

                    uiEntry.Panel.Color = color;
                    uiEntry.Panel.BorderColor = borderColor;
                    uiEntry.Panel.HoveredColor = hoverColor;

                    var msg = new FormattedMessage();
                    msg.AddText(name);
                    msg.PushNewline();

                    if (!string.IsNullOrWhiteSpace(entity.Description))
                        msg.AddText(entity.Description);

                    var tooltip = new Tooltip();
                    tooltip.SetMessage(msg);
                    tooltip.MaxWidth = 250f;

                    uiEntry.TooltipLabel.ToolTip = entity.Description;
                    uiEntry.TooltipLabel.TooltipDelay = 0;
                    uiEntry.TooltipLabel.TooltipSupplier = _ => tooltip;

                    uiEntry.Panel.Button.OnPressed += _ => OnButtonPressed(entry);
                }

                uiSection.Entries.AddChild(uiEntry);
            }

            _window.Sections.AddChild(uiSection);
        }

        _window.Express.OnPressed += _ => OnExpressDeliveryButtonPressed();
        _window.Search.OnTextChanged += OnSearchChanged;

        Refresh();

        _window.OpenCentered();
    }

    private void OnButtonPressed(Content.Shared.ADT.MiningShop.MiningShopEntry entry)
    {
        var msg = new MiningShopBuiMsg(entry);
        SendMessage(msg);
        Refresh();
    }

    private void OnExpressDeliveryButtonPressed()
    {
        var msg = new MiningShopExpressDeliveryBuiMsg();
        SendMessage(msg);
        Refresh();
    }

    private void OnSearchChanged(LineEditEventArgs args)
    {
        if (_window == null)
            return;

        foreach (var sectionControl in _window.Sections.Children)
        {
            if (sectionControl is not MiningShopSection section)
                continue;

            var any = false;
            foreach (var entriesControl in section.Entries.Children)
            {
                if (entriesControl is not MiningShopEntry entry)
                    continue;

                if (string.IsNullOrWhiteSpace(args.Text))
                    entry.Visible = true;
                else
                    entry.Visible = entry.Panel.Button.Label.Text?.Contains(args.Text, OrdinalIgnoreCase) ?? false;

                if (entry.Visible)
                    any = true;
            }

            section.Visible = any;
        }
    }

    public void Refresh()
    {
        if (_window == null || _player.LocalEntity == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out MiningShopComponent? vendor))
            return;

        List<string> names = new List<string>();

        foreach (var order in vendor.OrderList)
        {
            var name = _prototype.TryIndex(order.Id, out var entity) ? entity.Name : order.Name;
            if (name != null)
                names.Add(name);
        }
        var orders = string.Join(", ", names);

        var userpoints = _miningPoints.TryFindIdCard(_player.LocalEntity.Value)?.Comp?.Points ?? 0;

        _window.YourPurchases.Text = $"Заказы: {orders}";

        _window.Express.Text = $"Экспресс доставка";

        _window.PointsLabel.Text = $"Осталось очков: {userpoints}";

        var sections = _prototype.EnumeratePrototypes<SharedMiningShopSectionPrototype>();

        for (var sectionIndex = 0; sectionIndex < _sections.Count; sectionIndex++)
        {
            var section = _sections[sectionIndex];
            var uiSection = (MiningShopSection)_window.Sections.GetChild(sectionIndex);
            uiSection.Label.SetMessage(GetSectionName(section));

            var sectionDisabled = false;

            for (var entryIndex = 0; entryIndex < section.Entries.Count; entryIndex++)
            {
                var entry = section.Entries[entryIndex];
                var uiEntry = (MiningShopEntry)uiSection.Entries.GetChild(entryIndex);
                var disabled = sectionDisabled;

                if (userpoints < entry.Price)
                {
                    disabled = true;
                }

                uiEntry.Price.Text = $"{entry.Price}P";

                uiEntry.Panel.Button.Disabled = disabled;
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        switch (message)
        {
            case MiningShopRefreshBuiMsg:
                Refresh();
                break;
        }
    }

    private FormattedMessage GetSectionName(SharedMiningShopSectionPrototype section)
    {
        var name = new FormattedMessage();
        name.PushTag(new MarkupNode("bold", new MarkupParameter(section.Name.ToUpperInvariant()), null));
        name.AddText(section.Name.ToUpperInvariant());

        name.Pop();
        return name;
    }
}
