using Content.Client.UserInterface.Controls;
using Content.Shared.ADT.Morph;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client.ADT.Morph.UI;

public sealed partial class MimicryMenu : RadialMenu
{
    [Dependency] private readonly EntityManager _ent = default!;
    [Dependency] private readonly IPrototypeManager _prot = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public EntityUid Entity { get; private set; }

    public event Action<NetEntity>? SendActivateMessageAction;

    public MimicryMenu()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);
    }

    public void SetEntity(EntityUid ent)
    {
        Entity = ent;
        UpdateUI();
    }

    private void UpdateUI()
    {
        var main = FindControl<RadialContainer>("Main");
        if (main == null) return;
        main.RemoveAllChildren();

        var player = _player.LocalEntity;
        if (player == null)
            return;
        if (player == null)
            return;

        if (!_ent.TryGetComponent<MorphComponent>(player, out var morph))
            return;


        main.RemoveAllChildren();

        foreach (var morphable in morph.MemoryObjects)
        {
            var button = new EmbeddedEntityMenuButton
            {
                SetSize = new Vector2(64, 64),
                ToolTip = _ent.TryGetComponent<MetaDataComponent>(morphable, out var md) ? md.EntityName : "Unknown",
                NetEntity = md != null ? md.NetEntity : NetEntity.Invalid,
            };

            var texture = new SpriteView(morphable, _ent)
            {
                OverrideDirection = Direction.South,
                VerticalAlignment = VAlignment.Center,
                SetSize = new Vector2(64, 64),
                VerticalExpand = true,
                Stretch = SpriteView.StretchMode.Fill,
            };
            button.AddChild(texture);

            main.AddChild(button);
        }
        AddAction(main);
    }

    private void AddAction(RadialContainer main)
    {
        if (main == null)
            return;

        foreach (var child in main.Children)
        {
            var castChild = child as EmbeddedEntityMenuButton;
            if (castChild == null)
                continue;

            castChild.OnButtonUp += _ =>
            {
                SendActivateMessageAction?.Invoke(castChild.NetEntity);
                Close();
            };
        }
    }

    public sealed class EmbeddedEntityMenuButton : RadialMenuTextureButtonWithSector
    {
        public NetEntity NetEntity;
    }
}
