using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Silicons.Borgs;

public abstract class SharedBorgSwitchableSubtypeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableSubtypeComponent, BorgSelectSubtypeMessage>(OnSubtypeSelection);
        SubscribeLocalEvent<BorgSwitchableSubtypeComponent, BorgSubtypeChangedEvent>(OnSubtypeChanged);
    }

    private void OnSubtypeSelection(Entity<BorgSwitchableSubtypeComponent> ent, ref BorgSelectSubtypeMessage args)
    {
        SetSubtype(ent, args.Subtype);
    }

    public void SetSubtype(EntityUid ent, ProtoId<BorgSubtypePrototype> subtype)
    {
        if (!TryComp(ent, out BorgSwitchableSubtypeComponent? subtypeComp))
            return;

        subtypeComp.BorgSubtype = subtype;
        RaiseLocalEvent(ent, new BorgSubtypeChangedEvent(subtype));
    }

    private void OnSubtypeChanged(Entity<BorgSwitchableSubtypeComponent> ent, ref BorgSubtypeChangedEvent args)
    {
        UpdateAppearance(ent, args.Subtype);
    }

    private void UpdateAppearance(Entity<BorgSwitchableSubtypeComponent> ent, ProtoId<BorgSubtypePrototype> subtype)
    {
        if (!_proto.TryIndex(subtype, out var subtypePrototype))
            return;

        _appearance.SetData(ent, BorgSwitchableSubtypeUiKey.Key, subtypePrototype.Sprite);
    }
}

[Virtual]
public class BorgSubtypeChangedEvent : EntityEventArgs
{
    public ProtoId<BorgSubtypePrototype> Subtype { get; }

    public BorgSubtypeChangedEvent(ProtoId<BorgSubtypePrototype> subtype)
    {
        Subtype = subtype;
    }
}

[Serializable, NetSerializable]
public enum BorgSwitchableSubtypeUiKey : byte
{
    Key
}
