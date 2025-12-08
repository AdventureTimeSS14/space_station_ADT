using Content.Shared.ADT.SeedDna;
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

    private readonly Func<float?>? _getSeedPotency;
    private readonly Func<float?>? _getDiskPotency;

    private SeedDnaConsoleWindowRow(string title,
        bool seedPresent, bool dnaDiskPresent,
        Func<object?> getterSeedValue, Func<object?> getterDnaDiskValue,
        Action<object?> setterSeedValue, Action<object?> setterDnaDiskValue,
        Func<bool> flagUpdateImmediately,
        Action<TargetSeedData> submit,
        Func<float?>? getSeedPotency = null,
        Func<float?>? getDiskPotency = null
    )
    {
        _getSeedPotency = getSeedPotency;
        _getDiskPotency = getDiskPotency;

        SetupTitle(title);

        var seedValue = getterSeedValue();
        var diskValue = getterDnaDiskValue();

        var seedPotencyValue = _getSeedPotency?.Invoke();
        var diskPotencyValue = _getDiskPotency?.Invoke();

        SetLabelValue(_seedValueLabel = CreateValueLabel(), seedValue, seedPotencyValue);
        SetLabelValue(_dnaDiskValueLabel = CreateValueLabel(), diskValue, diskPotencyValue);

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

        // Removed sub-rows for chemicals as per modification

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
        Action<TargetSeedData> submit,
        Func<float?>? getSeedPotency = null,
        Func<float?>? getDiskPotency = null)
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
            submit,
            getSeedPotency,
            getDiskPotency);
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

        var targetPotencyFunc = target == TargetSeedData.Seed ? _getSeedPotency : _getDiskPotency;

        var action = () =>
        {
            var value = getter();
            if (value == null)
                return;

            setter(value);
            var targetPotency = targetPotencyFunc?.Invoke();
            SetLabelValue(setupLabel, value, targetPotency);
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

    private Label SetLabelValue(Label valueLabel, object? value, float? potency = null)
    {
        if (value == null)
        {
            valueLabel.Text = "-";
            valueLabel.Align = Label.AlignMode.Center;
        }
        else if (value is SeedChemQuantityDto chem)
        {
            _isChemical = true;
            if (potency == null)
            {
                valueLabel.Text = "-";
                valueLabel.Align = Label.AlignMode.Center;
            }
            else
            {
                var p = potency.Value;
                var x = chem.Min + (p / chem.PotencyDivisor);
                var amount = Math.Clamp(x, chem.Min, chem.Max);
                valueLabel.Text = amount.ToString() + "u";
                valueLabel.Align = Label.AlignMode.Right;
            }
        }
        else
        {
            valueLabel.Text = value.ToString();
            valueLabel.Align = Label.AlignMode.Right;
        }

        return valueLabel;
    }
}
