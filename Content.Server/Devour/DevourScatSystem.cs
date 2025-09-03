using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Devour.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Toilet.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Maths;
using System.Numerics;
using Robust.Shared.Random;
using Robust.Shared.Physics.Systems;
using Robust.Shared.GameObjects;
using System.Linq;
using System;

namespace Content.Server.Devour;

/// <summary>
/// System for handling the production and disposal of digested waste
/// </summary>
public sealed class DevourScatSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GutsComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ToiletComponent, InteractUsingEvent>(OnToiletInteract);
    }

    private void OnInteractUsing(EntityUid uid, GutsComponent component, InteractUsingEvent args)
    {
        // Check if the used item is a toilet
        if (!HasComp<ToiletComponent>(args.Used))
            return;

        // Attempt to use the toilet
        UseToilet(uid, component, args.Used);
        args.Handled = true;
    }

    private void OnToiletInteract(EntityUid uid, ToiletComponent component, InteractUsingEvent args)
    {
        // Check if the user has guts and wants to use the toilet
        if (!TryComp<GutsComponent>(args.User, out var guts))
            return;

        // Attempt to use the toilet
        UseToilet(args.User, guts, uid);
        args.Handled = true;
    }

    private void UseToilet(EntityUid user, GutsComponent guts, EntityUid toilet)
    {
        // Check if user produces scat
        if (TryComp<PredatorComponent>(user, out var predComp) && !predComp.ProducesScat)
            return;

        // Check if there's anything to dispose
        if (guts.CurrentAmount <= 0 && guts.GutsContainer.ContainedEntities.Count == 0)
            return;

        // Play toilet sound
        _audioSystem.PlayPvs(new SoundPathSpecifier("/Audio/Effects/toilet_flush.ogg"), toilet);

        // Dispose of waste
        DisposeWaste(user, guts, toilet);

        // Show message
        var message = Loc.GetString("devour-scat-toilet-used");
        _popupSystem.PopupEntity(message, user, user);
    }

    private void DisposeWaste(EntityUid user, GutsComponent guts, EntityUid toilet)
    {
        // Clear the guts container
        _containerSystem.EmptyContainer(guts.GutsContainer);

        // Clear the nutrient solution
        if (guts.NutrientSolution != null)
        {
            _solutionSystem.RemoveAllSolution(guts.NutrientSolution.Value);
        }

        // Reset current amount
        guts.CurrentAmount = 0;

        // Create scat entity if enabled
        if (TryComp<PredatorComponent>(user, out var predComp) && predComp.ProducesScat)
        {
            CreateScat(toilet);
        }
    }

    private void CreateScat(EntityUid toilet)
    {
        // Create a scat entity at the toilet location
        var scatProto = "DevourScat";

        if (_prototypeManager.TryIndex<EntityPrototype>(scatProto, out var prototype))
        {
            var scat = Spawn(scatProto, _transformSystem.GetMapCoordinates(toilet));

            // Add some random offset to make it look more realistic
            var offset = new Vector2(
                Random.Shared.NextFloat(-0.3f, 0.3f),
                Random.Shared.NextFloat(-0.3f, 0.3f)
            );

            if (TryComp<TransformComponent>(scat, out var transform))
            {
                _transformSystem.SetLocalPosition(scat, transform.LocalPosition + offset);
            }
        }
    }

    /// <summary>
    /// Force disposal of waste (e.g., when guts are full)
    /// </summary>
    public void ForceDisposal(EntityUid user, GutsComponent guts)
    {
        if (guts.CurrentAmount <= 0)
            return;

        // Find nearest toilet
        var toilets = EntityQuery<ToiletComponent>().ToList();
        var userPos = _transformSystem.GetMapCoordinates(user);

        EntityUid? nearestToilet = null;
        var nearestDistance = float.MaxValue;

        foreach (var toilet in toilets)
        {
            var toiletUid = toilet.Owner;
            var toiletPos = _transformSystem.GetMapCoordinates(toiletUid);
            var distance = Vector2.Distance(userPos.Position, toiletPos.Position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestToilet = toiletUid;
            }
        }

        if (nearestToilet != null)
        {
            UseToilet(user, guts, nearestToilet.Value);
        }
    }
}
