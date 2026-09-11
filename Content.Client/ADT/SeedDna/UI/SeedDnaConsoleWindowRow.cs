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

public sealed partial class SeedDnaConsoleWindowRow : BoxContainer
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static readonly Color LockedColor = new(0.45f, 0.45f, 0.45f, 0.5f);

    private static readonly Thickness ValueMargin = new(4, 0, 8, 0);

    private readonly string _geneId;

    private readonly Button _extractButton;
    private readonly Button _replaceButton;

    public SeedDnaConsoleWindowRow(SeedDnaGeneEntry entry, Action<string, SeedDnaTransferDirection, bool> onTransfer) : base()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Horizontal;
        SeparationOverride = 4;
        _geneId = entry.Id;

        var title = ResolveTitle(entry);

        if (entry.Cost > 0)
            title += $" ({Loc.GetString("seed-dna-row-cost", ("cost", entry.Cost))})";

        var titleLabel = new Label
        {
            Text = title,
            HorizontalExpand = true,
            ClipText = true,
            VerticalAlignment = VAlignment.Center,
        };

        var infoButton = new Button
        {
            Text = "?",
            MinSize = new Vector2(24, 24),
            VerticalAlignment = VAlignment.Center,
            TooltipDelay = 0,
        };

        if (entry.Description != null)
        {
            infoButton.ToolTip = Loc.GetString(entry.Description);
        }
        else
        {
            infoButton.Visible = false;
        }

        var seedValueLabel = CreateValueLabel(entry.SeedValue);
        var diskValueLabel = CreateValueLabel(entry.DiskValue);

        _extractButton = CreateActionButton(Loc.GetString("seed-dna-extract-btn"));
        _replaceButton = CreateActionButton(Loc.GetString("seed-dna-replace-btn"));

        _extractButton.Disabled = entry.SeedValue == null;
        _replaceButton.Disabled = entry.DiskValue == null;

        if (entry.LockedTech != null)
        {
            var lockedTip = Loc.GetString("seed-dna-row-locked", ("tech", entry.LockedTech));
            ApplyLockedStyle(_extractButton, lockedTip);
            ApplyLockedStyle(_replaceButton, lockedTip);
        }

        _extractButton.OnPressed += _ => onTransfer(_geneId, SeedDnaTransferDirection.SeedToDisk, false);
        _replaceButton.OnPressed += _ => onTransfer(_geneId, SeedDnaTransferDirection.DiskToSeed, false);

        AddChild(infoButton);
        AddChild(titleLabel);
        AddChild(seedValueLabel);
        AddChild(diskValueLabel);
        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
            Children =
            {
                _extractButton,
                _replaceButton,
            },
        });
    }

    public string GeneId => _geneId;
    public bool CanExtract => !_extractButton.Disabled;
    public bool CanReplace => !_replaceButton.Disabled;

    public void DisableButtons()
    {
        _extractButton.Disabled = true;
        _replaceButton.Disabled = true;
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
            VerticalAlignment = VAlignment.Center,
            MinWidth = 90,
            Margin = ValueMargin,
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
            var reagentId = entry.Id[SeedDnaGeneEntry.ChemicalPrefix.Length..];
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
