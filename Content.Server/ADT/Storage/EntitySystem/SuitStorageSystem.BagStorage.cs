using Content.Shared.ADT.ModSuits;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;

namespace Content.Server.ADT.Storage.System;

/// <summary>
/// МОД со стандартным рюкзачным модулем хранения подстраивается под то, что находится в слоте рюкзака:
/// Вещмешок - вещмешковый модуль. Рюкзак - рюкзачный модуль. Сумка - сумочный модуль. Если ничего не подходит - рюкзачный модуль
/// Модули, оверврайтнутые в прототипе МОДа, не трогаем.
/// </summary>
public sealed partial class SuitStorageSystem
{
    private static readonly EntProtoId DefaultStorageModule = "ADTModsuitModStorage";

    private const string DuffelParent = "ClothingBackpackDuffel";
    private const string SatchelParent = "ClothingBackpackSatchel";

    private static readonly EntProtoId DuffelStorageModule = "ADTModsuitModDuffelbagStorage";
    private static readonly EntProtoId SatchelStorageModule = "ADTModsuitModSatchelStorage";

    private void TryFitStorageModule(EntityUid suitUid, EntityUid user)
    {
        if (!TryComp<ModSuitComponent>(suitUid, out var suit))
            return;

        if (FindDefaultStorageModule((suitUid, suit)) is not { } module)
            return;

        var wanted = GetStorageModuleForBag(user);
        if (wanted == DefaultStorageModule)
            return;

        if (!TryComp<ModSuitModComponent>(module, out var oldMod))
            return;

        _modSuit.DeactivateModule((suitUid, suit), (module, oldMod));
        suit.CurrentComplexity -= oldMod.Complexity;
        _container.Remove(module, suit.ModuleContainer);
        QueueDel(module);

        var replacement = Spawn(wanted, MapCoordinates.Nullspace);
        if (!TryComp<ModSuitModComponent>(replacement, out var newMod))
        {
            QueueDel(replacement);
            return;
        }

        _container.Insert(replacement, suit.ModuleContainer);
        suit.CurrentComplexity += newMod.Complexity;

        if (newMod.IsInstantlyActive)
            _modSuit.ActivateModule((suitUid, suit), (replacement, newMod));

        Dirty(suitUid, suit);
    }

    private EntityUid? FindDefaultStorageModule(Entity<ModSuitComponent> suit)
    {
        foreach (var module in suit.Comp.ModuleContainer.ContainedEntities)
        {
            if (MetaData(module).EntityPrototype?.ID == DefaultStorageModule.Id)
                return module;
        }

        return null;
    }

    private EntProtoId GetStorageModuleForBag(EntityUid user)
    {
        if (!_inventory.TryGetSlotEntity(user, "back", out var bag))
            return DefaultStorageModule;

        var protoId = MetaData(bag.Value).EntityPrototype?.ID;
        if (protoId is null || !_proto.TryIndex<EntityPrototype>(protoId, out var proto))
            return DefaultStorageModule;

        if (InheritsFrom(proto, DuffelParent))
            return DuffelStorageModule;

        if (InheritsFrom(proto, SatchelParent))
            return SatchelStorageModule;

        return DefaultStorageModule;
    }

    /// Идем по цепочке родителей чтобы понять какой модуль нужен
 
    private bool InheritsFrom(EntityPrototype proto, string ancestor)
    {
        if (proto.ID == ancestor)
            return true;

        if (proto.Parents is not { } parents)
            return false;

        foreach (var parent in parents)
        {
            if (_proto.TryIndex<EntityPrototype>(parent, out var parentProto) && InheritsFrom(parentProto, ancestor))
                return true;
        }

        return false;
    }
}
