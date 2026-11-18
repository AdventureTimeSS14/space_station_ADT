using Content.Client.Guidebook.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Guidebook.Lazy;

public sealed class ADTLazySlot : Control, ISearchableControl, IPrototypeRepresentationControl
{
    private readonly ADTLazyList _list;
    private readonly string _searchText;
    private Func<Control>? _factory;
    private Control? _content;

    public IPrototype? RepresentedPrototype { get; }

    public bool IsMaterialized => _content != null;

    public ADTLazySlot(ADTLazyList list, Func<Control> factory, string searchText, IPrototype? prototype, float estimatedHeight)
    {
        _list = list;
        _factory = factory;
        _searchText = searchText;
        RepresentedPrototype = prototype;
        HorizontalExpand = true;
        MinHeight = estimatedHeight;
    }

    public void Materialize()
    {
        if (_factory == null)
            return;

        _content = _factory();
        _factory = null;
        MinHeight = 0;
        AddChild(_content);
        _list.OnMaterialized(_content);
    }

    public bool CheckMatchesSearch(string query)
    {
        if (_content is ISearchableControl searchable)
            return searchable.CheckMatchesSearch(query);

        return _searchText.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    public void SetHiddenState(bool state, string query)
    {
        Visible = CheckMatchesSearch(query) ? state : !state;
        _list.QueueCheck();
    }
}
