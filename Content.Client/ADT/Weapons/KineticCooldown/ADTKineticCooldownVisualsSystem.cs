using Content.Client.Cooldown;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Shared.ADT.Weapons.KineticCooldown;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Weapons.KineticCooldown;

public sealed class ADTKineticCooldownVisualsSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private const string GraphicName = "ADTKineticCooldownGraphic";

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_ui.GetActiveUIWidgetOrNull<HotbarGui>() is not { } hotbar)
            return;

        foreach (var hand in hotbar.HandContainer.GetButtons())
        {
            var graphic = FindGraphic(hand);

            if (!TryComp<ADTKineticCooldownComponent>(hand.Entity, out var cooldown))
            {
                if (graphic != null)
                    graphic.Visible = false;

                continue;
            }

            graphic ??= AddGraphic(hand);
            if (cooldown.NextUse <= cooldown.LastUseStart)
            {
                graphic.Visible = false;
                continue;
            }

            graphic.FromTime(cooldown.LastUseStart, cooldown.NextUse);
        }
    }

    private CooldownGraphic? FindGraphic(Control slot)
    {
        foreach (var child in slot.Children)
        {
            if (child is CooldownGraphic graphic && graphic.Name == GraphicName)
                return graphic;
        }

        return null;
    }

    private CooldownGraphic AddGraphic(Control slot)
    {
        var graphic = new CooldownGraphic
        {
            Name = GraphicName,
            Visible = false,
        };

        slot.AddChild(graphic);
        return graphic;
    }
}
