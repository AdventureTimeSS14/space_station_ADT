using Content.Client.UserInterface.Controls;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Messages;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Heretic.UI;

[UsedImplicitly]
public sealed class MawedCrucibleBoundUserInterface : BoundUserInterface
{
    [Dependency] private IPrototypeManager _proto = default!;

    private SimpleRadialMenu? _menu;

    public MawedCrucibleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent(Owner, out MawedCrucibleComponent? crucible))
            return;

        _menu = new SimpleRadialMenu();
        _menu.OnClose += Close;
        var buttonModels = ConvertToButtons(crucible.Potions);
        _menu.SetButtons(buttonModels);

        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuActionOption<EntProtoId>> ConvertToButtons(IReadOnlyList<EntProtoId> entProtoIds)
    {
        var models = new RadialMenuActionOption<EntProtoId>[entProtoIds.Count];
        for (var i = 0; i < entProtoIds.Count; i++)
        {
            var protoId = entProtoIds[i];
            var proto = _proto.Index(protoId);
            models[i] = new RadialMenuActionOption<EntProtoId>(HandleRadialMenuClick, protoId)
            {
                IconSpecifier = new RadialMenuEntityPrototypeIconSpecifier(protoId),
                ToolTip = proto.Name,
            };
        }

        return models;
    }

    private void HandleRadialMenuClick(EntProtoId proto)
    {
        SendPredictedMessage(new MawedCrucibleMessage(proto));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_menu != null)
        {
            _menu.OnClose -= Close;
            _menu.Dispose();
        }
    }
}
