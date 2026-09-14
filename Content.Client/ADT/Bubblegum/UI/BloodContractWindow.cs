using System.Numerics;
using Content.Shared.ADT.Bubblegum.Loot;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.ADT.Bubblegum.UI;

public sealed class BloodContractWindow : DefaultWindow
{
    private readonly BoxContainer _list;

    public event Action<NetEntity>? OnTargetSelected;

    public BloodContractWindow()
    {
        Title = Loc.GetString("adt-blood-contract-window-title");
        MinSize = new Vector2(280, 360);

        _list = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
        };

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        scroll.AddChild(_list);
        Contents.AddChild(scroll);
    }

    public void SetTargets(List<BloodContractTargetInfo> targets)
    {
        _list.RemoveAllChildren();

        foreach (var target in targets)
        {
            var button = new Button
            {
                Text = target.Name,
                HorizontalExpand = true,
            };
            button.OnPressed += _ => OnTargetSelected?.Invoke(target.Entity);
            _list.AddChild(button);
        }
    }
}
