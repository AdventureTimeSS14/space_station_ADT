using Content.Shared.ADT.SeedDna;
using Content.Shared.ADT.SeedDna.Prototypes;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client.ADT.SeedDna.UI;

public sealed class SeedDnaConsoleWindowRow
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static readonly Color LockedColor = new(0.45f, 0.45f, 0.45f, 0.5f);

    private readonly string _geneId;

    private readonly Button _infoButton;
    private readonly Label _titleLabel;
    private readonly Label _seedValueLabel;
    private readonly Label _diskValueLabel;
    private readonly Button _extractButton;
    private readonly Button _replaceButton;

    private Container? _container;
    private readonly List<Control> _cells = [];

    public SeedDnaConsoleWindowRow(SeedDnaGeneEntry entry, Action<string, SeedDnaTransferDirection> onTransfer)
    {
        IoCManager.InjectDependencies(this);

        _geneId = entry.Id;

        var title = ResolveTitle(entry);

        if (entry.Cost > 0)
            title += $" ({Loc.GetString("seed-dna-row-cost", ("cost", entry.Cost))})";

        _titleLabel = new Label
        {
            Text = title,
            HorizontalExpand = true,
            ClipText = true,
            VerticalAlignment = Control.VAlignment.Center,
        };

        _infoButton = new Button
        {
            Text = "?",
            MinSize = new Vector2(24, 24),
            VerticalAlignment = Control.VAlignment.Center,
            TooltipDelay = 0,
        };

        if (entry.Description != null)
        {
            _infoButton.ToolTip = Loc.GetString(entry.Description);
        }
        else
        {
            _infoButton.Disabled = true;
        }

        _seedValueLabel = CreateValueLabel(entry.SeedValue);
        _diskValueLabel = CreateValueLabel(entry.DiskValue);

        _extractButton = CreateActionButton(Loc.GetString("seed-dna-extract-btn"));
        _replaceButton = CreateActionButton(Loc.GetString("seed-dna-replace-btn"));

        _extractButton.Disabled = entry.LockedTech != null || entry.SeedValue == null;
        _replaceButton.Disabled = entry.LockedTech != null || entry.DiskValue == null;

        if (entry.LockedTech != null)
        {
            var lockedTip = Loc.GetString("seed-dna-row-locked", ("tech", entry.LockedTech));
            ApplyLockedStyle(_extractButton, lockedTip);
            ApplyLockedStyle(_replaceButton, lockedTip);
        }

        _extractButton.OnPressed += _ => onTransfer(_geneId, SeedDnaTransferDirection.SeedToDisk);
        _replaceButton.OnPressed += _ => onTransfer(_geneId, SeedDnaTransferDirection.DiskToSeed);
    }

    public string GeneId => _geneId;
    public bool CanExtract => !_extractButton.Disabled;
    public bool CanReplace => !_replaceButton.Disabled;

    public void DisableButtons()
    {
        _extractButton.Disabled = true;
        _replaceButton.Disabled = true;
    }

    public void IncludeToContainer(Container container)
    {
        _container = container;

        _cells.Add(_infoButton);
        _cells.Add(_titleLabel);
        _cells.Add(_seedValueLabel);
        _cells.Add(_diskValueLabel);
        _cells.Add(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
            Children =
            {
                _extractButton,
                _replaceButton,
            },
        });

        foreach (var cell in _cells)
            container.AddChild(cell);
    }

    public void Remove()
    {
        if (_container == null)
            return;

        foreach (var cell in _cells)
            _container.RemoveChild(cell);

        _cells.Clear();
        _container = null;
    }

    private static void ApplyLockedStyle(Button button, string tooltip)
    {
        button.ToolTip = tooltip;
        button.TooltipDelay = 0;
        button.ModulateSelfOverride = LockedColor;
    }

    private static Label CreateValueLabel(string? value)
    {
        return new Label
        {
            Text = value ?? "-",
            StyleClasses = { "monospace" },
            Align = Label.AlignMode.Right,
            VerticalAlignment = Control.VAlignment.Center,
            MinWidth = 90,
        };
    }

    private static Button CreateActionButton(string text)
    {
        return new Button
        {
            Text = text,
        };
    }

    private string ResolveTitle(SeedDnaGeneEntry entry)
    {
        if (entry.Type == SeedDnaGeneType.Chemical)
        {
            var reagentId = entry.Id.StartsWith(SeedDnaGeneEntry.ChemicalPrefix)
                ? entry.Id[SeedDnaGeneEntry.ChemicalPrefix.Length..]
                : entry.Id;

            if (_proto.TryIndex<ReagentPrototype>(reagentId, out var reagent))
                return reagent.LocalizedName;

            return reagentId;
        }

        if (entry.GasName != null)
        {
            var gasName = entry.GasName;
            if (_proto.TryIndex<GasPrototype>(gasName, out var gasProto)
                && Loc.TryGetString(gasProto.Name, out var localized))
                gasName = localized;

            return Loc.GetString(entry.Name, ("gas", gasName));
        }

        return Loc.GetString(entry.Name);
    }
}
