using System.Linq;
using Content.Client.Chemistry.EntitySystems;
using Content.Shared.Medical.Healing;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Kitchen;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._NF.Medical.EntitySystems;

public sealed class MedicalRecipeDataSystem : SharedMedicalGuideDataSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    private Dictionary<string, List<MedicalRecipeData>> _sources = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        _player.PlayerStatusChanged += OnPlayerStatusChanged;

        ReloadRecipes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<EntityPrototype>()
            && !args.WasModified<FoodRecipePrototype>()
        )
            return;

        ReloadRecipes();
    }

    public void ReloadRecipes()
    {
        _sources.Clear();

        foreach (var recipe in _protoMan.EnumeratePrototypes<FoodRecipePrototype>())
        {
            if (recipe.HideInGuidebook)
                continue;

            MicrowaveRecipeType recipeType = (MicrowaveRecipeType)recipe.RecipeType;
            if (recipeType.HasFlag(MicrowaveRecipeType.MedicalAssembler))
            {
                _sources.GetOrNew(recipe.Result).Add(new MedicalRecipeData(recipe));
            }
        }

        Registry.Clear();

        foreach (var (result, sources) in _sources)
        {
            var proto = _protoMan.Index<EntityPrototype>(result);
            ReagentQuantity[] reagents = [];
            if (proto.TryGetComponent<SolutionComponent>(out var selfSolution, _componentFactory))
            {
                reagents = selfSolution.Solution.Contents.ToArray();
            }
            else
            {
                foreach (var (_, solution) in _solutionContainer.EnumerateSolutions(proto))
                {
                    reagents = solution.Contents.ToArray();
                    break;
                }
            }

            DamageSpecifier? damage = null;
            if (proto.TryGetComponent<HealingComponent>(out var healing, _componentFactory))
                damage = healing.Damage;

            // Limit the number of sources to 10 - shouldn't be an issue for medical recipes, but just in case.
            var distinctSources = sources.DistinctBy(it => it.Identitier).Take(10);

            var entry = new MedicalGuideEntry(result, proto.Name, distinctSources.ToArray(), reagents, damage);
            Registry.Add(entry);
        }

        RaiseNetworkEvent(new MedicalGuideRegistryChangedEvent(Registry));
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.Connected)
            return;

        RaiseNetworkEvent(new MedicalGuideRegistryChangedEvent(Registry), args.Session);
    }
}
