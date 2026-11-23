using System.Linq;
using Content.Client.UserInterface.ControlExtensions;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.Guidebook;

public sealed class ADTGuidebookCategoryMenu : OptionButton
{
    private readonly Label _caption;

    public ADTGuidebookCategoryMenu()
    {
        PrefixMargin = false;
        _caption = this.GetControlOfType<Label>(true).First();
        _caption.ClipText = true;
        _caption.HorizontalExpand = true;
        OnItemSelected += _ => UpdateTooltip();
    }

    public new void SelectId(int id)
    {
        base.SelectId(id);
        UpdateTooltip();
    }

    public override void ButtonOverride(Button button)
    {
        button.ToolTip = button.Text;
    }

    private void UpdateTooltip()
    {
        ToolTip = _caption.Text;
    }
}
