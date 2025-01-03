using Content.Client.DamageState;
using Content.Client.Humanoid;
using Content.Shared.ADT.Salvage;
using Content.Shared.ADT.Salvage.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.ADT.Salvage;

public sealed class MegafaunaVisualsSystem : EntitySystem
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

        SubscribeLocalEvent<MegafaunaComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, MegafaunaComponent comp, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        if (_appearance.TryGetData<bool>(uid, AshdrakeVisuals.Swoop, out var swoop))
        {
            sprite.LayerSetState(DamageStateVisualLayers.Base, swoop ? "swoop" : "dragon");
        }
    }
}
