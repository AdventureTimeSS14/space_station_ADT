// ADT-Tweak
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Polymorph.Systems;

public sealed partial class PolymorphSystem
{
    [Dependency] private readonly IComponentFactory _goobCompFact = default!;

    // goob edit
    // it makes more sense for it to be here than anywhere.
    // if anywhere it should be embedded in the engine but we can't afford that :P
    public T? CopyPolymorphComponent<T>(EntityUid old, EntityUid @new, bool transfer = true) where T : Component
        => CopyPolymorphComponent(old, @new, typeof(T), transfer) as T;

    // don't use transfer if you have component references like EE languages
    // ideally you shouldn't use comp references at all
    public IComponent? CopyPolymorphComponent(EntityUid old, EntityUid @new, string componentRegistration, bool transfer = true)
    {
        if (!_goobCompFact.TryGetRegistration(componentRegistration, out var reg))
            return null;

        return CopyPolymorphComponent(old, @new, reg.Type, transfer);
    }

    public IComponent? CopyPolymorphComponent(EntityUid old, EntityUid @new, Type compType, bool transfer = true)
    {
        if (old == @new)
            return null;

        if (!EntityManager.TryGetComponent(old, compType, out var comp))
            return null;

        if (transfer)
        {
            var newComp = (Component) _goobCompFact.GetComponent(compType);
            var temp = (object) newComp;
            _serialization.CopyTo(comp, ref temp, notNullableOverride: true);
            EntityManager.AddComponent(@new, (Component) temp!, true);
            return temp as IComponent;
        }

        var copy = _serialization.CreateCopy(comp, notNullableOverride: true);
        AddComp(@new, copy, true);
        return copy;
    }
    // goob edit end
}
