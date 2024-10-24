/// Made for Adventure Time Project by ModerN. https://github.com/modern-nm mailto:modern-nm@yandex.by
/// see also https://github.com/DocNITE/liebendorf-station/tree/feature/emote-radial-panel
/// /// using Content.Client.Humanoid;
using Content.Client.UserInterface.Systems.Radial;
using Content.Client.UserInterface.Systems.Radial.Controls;
using Content.Shared.Changeling;
using Content.Shared.Humanoid.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Content.Client.UserInterface.Controls;

namespace Content.Client.ADT.Language;

public sealed class ChangelingPanelSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _appearanceSystem = default!;

    /// <summary>
    /// We should enable radial for single target
    /// </summary>
    private RadialContainer? _openedMenu;

    private const string DefaultIcon = "/Textures/Interface/AdminActions/play.png";

    private const string EmptyIcon = "/Textures/Interface/AdminActions/emptyIcon.png";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeNetworkEvent<RequestChangelingFormsMenuEvent>(HandleChangelingFormsMenuEvent);
    }

    private void HandleChangelingFormsMenuEvent(RequestChangelingFormsMenuEvent args)
    {
        if (_openedMenu != null)
            return;
        if (_playerMan.LocalEntity == null)
        {
            return;
        }

        //if (!TryComp<ChangelingComponent>(_playerMan.LocalEntity.Value, out var changelingComponent)) // нет на клиенте
        //    return;

        _openedMenu = _userInterfaceManager.GetUIController<RadialUiController>()
            .CreateRadialContainer();

        foreach (var humanoid in args.HumanoidData)
        {
            var dummy = _entManager.SpawnEntity(_proto.Index<SpeciesPrototype>(humanoid.Species).DollPrototype, MapCoordinates.Nullspace);
            //var humanoidEntityUid = GetEntity(humanoid); // Entities on the client outside of the FOV are nonexistant. You can see that if you zoom out. //So it'll give you UID 0 which is EntityUid.Invalid.
            _appearanceSystem.LoadProfile(dummy, humanoid.Profile);
            var face = new SpriteView();
            face.SetEntity(dummy);

            var actionName = humanoid.Name;
            var texturePath = _spriteSystem.Frame0(new SpriteSpecifier.Texture(new ResPath(EmptyIcon)));

            var emoteButton = _openedMenu.AddButton(actionName, texturePath, face);
            emoteButton.Opacity = 210;
            emoteButton.Tooltip = null;
            emoteButton.Controller.OnPressed += (_) =>
            {
                var ev = new SelectChangelingFormEvent(args.Target, entitySelected: humanoid.NetEntity);
                RaiseNetworkEvent(ev);
                _openedMenu.Dispose();
            };
        }
        _openedMenu.OnClose += (_) =>
        {
            _openedMenu = null;
        };
        if (_playerMan.LocalEntity != null)
            _openedMenu.OpenAttached(_playerMan.LocalEntity.Value);

    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        _openedMenu?.Dispose();
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        _openedMenu?.Dispose();
    }
}
