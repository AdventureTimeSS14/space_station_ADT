using Content.Client.ADT.InconnuOS.UI.Apps;
using Content.Shared.ADT.InconnuOS;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Content.Client.ADT.InconnuOS.UI;

public sealed class OsAppRegistry
{
    private readonly IDynamicTypeFactory _factory;
    private readonly Dictionary<string, Type> _apps = new();

    public OsAppRegistry(IReflectionManager reflection, IDynamicTypeFactory factory)
    {
        _factory = factory;

        foreach (var type in reflection.GetAllChildren<OsAppControl>())
        {
            if (type.IsAbstract)
                continue;

            _apps[type.Name] = type;
        }
    }

    public bool Has(ADTOsAppPrototype proto)
    {
        return _apps.ContainsKey(proto.Window);
    }

    public OsAppControl Create(ADTOsAppPrototype proto)
    {
        if (!_apps.TryGetValue(proto.Window, out var type))
            type = typeof(OsPlaceholderApp);

        return (OsAppControl) _factory.CreateInstance(type);
    }
}
