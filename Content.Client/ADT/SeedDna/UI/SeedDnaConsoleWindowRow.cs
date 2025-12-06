using Content.Shared.ADT.SeedDna;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.SeedDna.UI;

public sealed class SeedDnaConsoleWindowRow
{
    // см. примечание в SeedDnaConsoleWindow.xaml
    private const float MarginValue = 15f / 2f;
    private static readonly Thickness LeftMargin = new(0, 0, MarginValue, 0);
    private static readonly Thickness LeftRightMargin = new(MarginValue, 0);
    private static readonly Thickness RightMargin = new(MarginValue, 0, 0, 0);

    private bool _isChemical;

    private Label? _titleLabel;
    private Label? _seedValueLabel;
    private Label? _dnaDiskValueLabel;
    private Button? _extractButton;
    private Button? _replaceButton;

    private Func<SeedChemQuantityDto?>? _getterChemSeedValue;
    private Func<SeedChemQuantityDto?>? _getterChemDnaDiskValue;

    private Action? _actionExtract;
    private Action? _actionReplace;

    private SeedDnaConsoleWindowRow(string title,
        bool seedPresent, bool dnaDiskPresent,
        Func<object?> getterSeedValue, Func<object?> getterDnaDiskValue,
        Action<object?> setterSeedValue, Action<object?> setterDnaDiskValue,
        Func<bool> flagUpdateImmediately,
        Action<TargetSeedData> submit
    )
    {
        SetupTitle(title);
        SetLabelValue(_seedValueLabel = CreateValueLabel(), getterSeedValue());
        SetLabelValue(_dnaDiskValueLabel = CreateValueLabel(), getterDnaDiskValue());

        _actionExtract = SetupActionButton(_extractButton = CreateActionButton(Loc.GetString("seed-dna-extract-btn")),
            dnaDiskPresent, getterSeedValue, setterDnaDiskValue,
            _seedValueLabel, _dnaDiskValueLabel,
            flagUpdateImmediately,
            submit, TargetSeedData.DnaDisk);

        _actionReplace = SetupActionButton(_replaceButton = CreateActionButton(Loc.GetString("seed-dna-replace-btn")),
            seedPresent, getterDnaDiskValue, setterSeedValue,
            _dnaDiskValueLabel, _seedValueLabel,
            flagUpdateImmediately,
            submit, TargetSeedData.Seed);

        if (!_isChemical)
            return;

        _getterChemSeedValue = () => (SeedChemQuantityDto?)getterSeedValue();
        _getterChemDnaDiskValue = () => (SeedChemQuantityDto?)getterDnaDiskValue();
    }

    public SeedDnaConsoleWindowRow IncludeToContainer(Container container)
    {
        container.AddChild(_titleLabel!);
        container.AddChild(_seedValueLabel!);
        container.AddChild(_dnaDiskValueLabel!);
        container.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            Margin = RightMargin,
            Children =
            {
                _extractButton!,
                _replaceButton!,
            },
        });

        if (!_isChemical)
            return this;

        var seedChemQuantityDto = _getterChemSeedValue!();
        var dnaDiskChemQuantityDto = _getterChemDnaDiskValue!();

        container.AddChild(CreateTitleLabel($"  - {Loc.GetString("seed-dna-chemicalProp-Min")}"));
        container.AddChild(SetLabelValue(CreateValueLabel(), seedChemQuantityDto?.Min));
        container.AddChild(SetLabelValue(CreateValueLabel(), dnaDiskChemQuantityDto?.Min));
        container.AddChild(new Control());

        container.AddChild(CreateTitleLabel($"  - {Loc.GetString("seed-dna-chemicalProp-Max")}"));
        container.AddChild(SetLabelValue(CreateValueLabel(), seedChemQuantityDto?.Max));
        container.AddChild(SetLabelValue(CreateValueLabel(), dnaDiskChemQuantityDto?.Max));
        container.AddChild(new Control());

        container.AddChild(CreateTitleLabel($"  - {Loc.GetString("seed-dna-chemicalProp-PotencyDivisor")}"));
        container.AddChild(SetLabelValue(CreateValueLabel(), seedChemQuantityDto?.PotencyDivisor));
        container.AddChild(SetLabelValue(CreateValueLabel(), dnaDiskChemQuantityDto?.PotencyDivisor));
        container.AddChild(new Control());

        container.AddChild(CreateTitleLabel($"  - {Loc.GetString("seed-dna-chemicalProp-Inherent")}"));
        container.AddChild(SetLabelValue(CreateValueLabel(), seedChemQuantityDto?.Inherent));
        container.AddChild(SetLabelValue(CreateValueLabel(), dnaDiskChemQuantityDto?.Inherent));
        container.AddChild(new Control());

        return this;
    }

    public void DoExtract()
    {
        _actionExtract!();
    }

    public void DoReplace()
    {
        _actionReplace!();
    }

    public static SeedDnaConsoleWindowRow? Create(string title,
        bool seedPresent, bool dnaDiskPresent,
        Func<object?> getterSeedValue, Func<object?> getterDnaDiskValue,
        Action<object?> setterSeedValue, Action<object?> setterDnaDiskValue,
        Func<bool> flagUpdateImmediately,
        Action<TargetSeedData> submit)
    {
        if (getterSeedValue() == null && getterDnaDiskValue() == null)
            return null;

        return new SeedDnaConsoleWindowRow(title,
            seedPresent,
            dnaDiskPresent,
            getterSeedValue,
            getterDnaDiskValue,
            setterSeedValue,
            setterDnaDiskValue,
            flagUpdateImmediately,
            submit);
    }

    private void SetupTitle(string title)
    {
        _titleLabel = CreateTitleLabel(title);
    }

    private Action SetupActionButton(Button actionBtn,
        bool secondDataPresent, Func<object?> getter, Action<object?> setter,
        Label getupLabel, Label setupLabel,
        Func<bool> flagUpdateImmediately,
        Action<TargetSeedData> submit, TargetSeedData target)
    {
        actionBtn.Disabled = !(getter() != null && secondDataPresent) && !getupLabel.Text!.Equals(setupLabel.Text);

        var action = () =>
        {
            var value = getter();
            if (value == null)
                return;

            setter(value);
            SetLabelValue(setupLabel, getupLabel.Text);
            _extractButton!.Disabled = true;
            _replaceButton!.Disabled = true;

            if (flagUpdateImmediately())
                submit(target);
        };

        actionBtn.OnPressed += _ => { action(); };

        return action;
    }

    private Label CreateTitleLabel(string title)
    {
        return new Label { Text = title, Margin = LeftMargin };
    }

    private Label CreateValueLabel()
    {
        return new Label
        {
            StyleClasses = { "monospace" },
            Margin = LeftRightMargin,
        };
    }

    private Button CreateActionButton(string title)
    {
        return new Button
        {
            Text = title,
        };
    }

    private Label SetLabelValue(Label valueLabel, object? value)
    {
        if (value == null)
        {
            valueLabel.Text = "-";
            valueLabel.Align = Label.AlignMode.Center;
        }
        else if (value is SeedChemQuantityDto)
        {
            _isChemical = true;
            valueLabel.Text = ".";
            valueLabel.Align = Label.AlignMode.Right;
        }
        else
        {
            valueLabel.Text = value.ToString();
            valueLabel.Align = Label.AlignMode.Right;
        }

        return valueLabel;
    }
}
