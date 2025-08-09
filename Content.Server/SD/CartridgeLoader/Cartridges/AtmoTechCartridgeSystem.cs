using Content.Shared.Atmos.Components;
using Content.Shared.CartridgeLoader;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class AtmoTechCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmoTechCartridgeComponent, CartridgeAddedEvent>(OnCartridgeAdded);
        SubscribeLocalEvent<AtmoTechCartridgeComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);
    }

    private void OnCartridgeAdded(Entity<AtmoTechCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        var gasAnalyzer = EnsureComp<GasAnalyzerComponent>(args.Loader);
    }

    private void OnCartridgeRemoved(Entity<AtmoTechCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        // only remove when the program itself is removed
        if (!_cartridgeLoaderSystem.HasProgram<AtmoTechCartridgeComponent>(args.Loader))
        {
            RemComp<GasAnalyzerComponent>(args.Loader);
        }
    }
}
