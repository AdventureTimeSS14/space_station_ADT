using Content.Shared.Bible.Components;
using Content.Shared.ADT.Phantom.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Client.UserInterface;
using Content.Shared.StatusIcon.Components;
using Content.Client.Humanoid;

namespace Content.Client.Chaplain;

public sealed class ChaplainSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
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
