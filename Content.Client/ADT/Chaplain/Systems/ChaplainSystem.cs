using Content.Shared.ADT.Chaplain.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Client.UserInterface;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Chaplain;

public sealed class ChaplainSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private IPlayerManager _playerMan = default!;
    [Dependency] private SpriteSystem _spriteSystem = default!;
    [Dependency] private IEntityManager _entManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChaplainComponent, GetStatusIconsEvent>(GetStatusIcon);
    }

    private void GetStatusIcon(EntityUid uid, ChaplainComponent component, ref GetStatusIconsEvent args)
    {
        if (_proto.TryIndex(component.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
