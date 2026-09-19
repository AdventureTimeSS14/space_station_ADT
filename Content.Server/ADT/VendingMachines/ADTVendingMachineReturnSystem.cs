using System.Linq;
using System.Numerics;
using Content.Server.Power.EntitySystems;
using Content.Shared.ADT.VendingMachines;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.VendingMachines;

public sealed class ADTVendingMachineReturnSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly VendingMachineSystem _vending = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VendingMachineComponent, ADTVendingReturnedEjectEvent>(OnReturnedEject);
        SubscribeLocalEvent<VendingMachineComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    public bool TryReturnItem(EntityUid uid, VendingMachineComponent component, EntityUid user, EntityUid used)
    {
        if (HasComp<StealTargetComponent>(used) || ContainsStealTarget(used))
        {
            Deny(uid, component);
            return false;
        }

        var protoId = MetaData(used).EntityPrototype?.ID;
        if (string.IsNullOrEmpty(protoId) || !component.Inventory.ContainsKey(protoId))
        {
            Deny(uid, component);
            return false;
        }

        _popup.PopupEntity(
            Loc.GetString("vending-machine-return-success", ("item", Identity.Entity(used, EntityManager))),
            uid, user);

        var data = new ReturnedItemData
        {
            Label = _label.GetLabelText(used),
            PaintColor = TryComp<ADTClothingPaintComponent>(used, out var paint) ? paint.PaintColor : null,
            Solution = ExtractSolution(used),
        };

        if (!component.ReturnedItems.TryGetValue(protoId, out var list))
        {
            list = new();
            component.ReturnedItems[protoId] = list;
        }

        list.Add(data);
        Dirty(uid, component);
        _vending.UpdateVendingMachineInterfaceState(uid, component);

        _audio.PlayPvs(component.SoundInsertCurrency, uid);
        Del(used);
        return true;
    }

    private Solution? ExtractSolution(EntityUid item)
    {
        if (!TryComp<SolutionContainerManagerComponent>(item, out var manager))
            return null;

        string? preferredName = null;
        if (TryComp<SolutionContainerVisualsComponent>(item, out var visuals))
            preferredName = visuals.SolutionName;

        foreach (var (name, _) in _solutionContainer.EnumerateSolutions((item, manager)))
        {
            if (preferredName != null && name != preferredName)
                continue;

            if (_solutionContainer.TryGetSolution(item, name, out _, out var solution))
                return solution.Clone();
        }

        return null;
    }

    private void OnGetVerbs(EntityUid uid, VendingMachineComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using is not { } used)
            return;

        if (component.Broken || !this.IsPowered(uid, EntityManager))
            return;

        var protoId = MetaData(used).EntityPrototype?.ID;
        if (string.IsNullOrEmpty(protoId) || !component.Inventory.ContainsKey(protoId))
            return;

        Verb verb = new()
        {
            Text = Loc.GetString("vending-machine-return-verb"),
            Category = VerbCategory.Insert,
            Act = () => TryReturnItem(uid, component, args.User, used),
        };
        args.Verbs.Add(verb);
    }
    private bool ContainsStealTarget(EntityUid item)
    {
        if (!TryComp<StorageComponent>(item, out var storage) || storage.Container == null)
            return false;

        return storage.Container.ContainedEntities.Any(e => HasComp<StealTargetComponent>(e));
    }

    private void OnReturnedEject(EntityUid uid, VendingMachineComponent component, ADTVendingReturnedEjectEvent args)
    {
        if (!component.ReturnedItems.TryGetValue(args.ItemProtoId, out var list) || list.Count == 0)
            return;

        if (!_prototypeManager.HasIndex<EntityPrototype>(args.ItemProtoId))
            return;

        for (var i = 0; i < args.Count && list.Count > 0; i++)
        {
            var data = list[^1];
            list.RemoveAt(list.Count - 1);

            var ent = Spawn(args.ItemProtoId, args.Coordinates);
            ApplyReturnedData(ent, data, args.PaintColor);

            if (args.ThrowItem)
            {
                var range = component.NonLimitedEjectRange;
                var direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                _throwingSystem.TryThrow(ent, direction, component.NonLimitedEjectForce);
            }
        }

        if (list.Count == 0)
            component.ReturnedItems.Remove(args.ItemProtoId);

        Dirty(uid, component);
    }

    private void ApplyReturnedData(EntityUid ent, ReturnedItemData data, Color? requestedPaint)
    {
        if (data.Label is { Length: > 0 } label)
            _label.Label(ent, label);

        PaintClothing(ent, requestedPaint ?? data.PaintColor);

        if (data.Solution is { } solution)
            RestoreSolution(ent, solution);
    }

    private void RestoreSolution(EntityUid ent, Solution stored)
    {
        if (!TryComp<SolutionContainerManagerComponent>(ent, out var manager))
            return;

        string? solutionName = null;
        if (TryComp<SolutionContainerVisualsComponent>(ent, out var visuals))
            solutionName = visuals.SolutionName;

        if (solutionName == null
            || !_solutionContainer.TryGetSolution(ent, solutionName, out var soln, out _))
        {
            return;
        }

        _solutionContainer.RemoveAllSolution(soln.Value);
        _solutionContainer.AddSolution(soln.Value, stored);
    }

    private void Deny(EntityUid uid, VendingMachineComponent component)
    {
        _vending.Deny(uid, component);
    }

    public void PaintClothing(EntityUid uid, Color? color)
    {
        if (!HasComp<ClothingComponent>(uid))
            return;

        if (color is { } paintColor)
        {
            var paint = EnsureComp<ADTClothingPaintComponent>(uid);
            paint.PaintColor = paintColor;
            Dirty(uid, paint);
        }
        else if (TryComp<ADTClothingPaintComponent>(uid, out var paint))
        {
            paint.PaintColor = null;
            Dirty(uid, paint);
        }
    }
}