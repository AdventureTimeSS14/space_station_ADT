using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Prototypes;
using Content.Client.ADT.Phantom.UI;
using Content.Shared.ADT.Phantom;
using Robust.Shared.Player;
using Content.Shared.Preferences;

namespace Content.Client.UserInterface.Systems.Phantom;

[UsedImplicitly]
public sealed class PhantomRadialUIController : UIController
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private PhantomRadialMenu? _menu;

    public void OpenMenu()
    {
        if (_menu != null)
            return;

        // setup window
        _menu = UIManager.CreateWindow<PhantomRadialMenu>();
        _menu.OnClose += CloseMenu;
        _menu.OnSelectStyle += OnSelectStyle;
        _menu.OnSelectFreedom += OnSelectFreedom;
        _menu.OnSelectVessel += OnSelectVessel;

        _menu.OpenCentered();
    }

    public void PopulateStyles()
        => _menu?.PopulateStyles();

    public void PopulateVessels(List<(NetEntity, HumanoidCharacterProfile, string)> vessels)
        => _menu?.PopulateVessels(vessels);

    public void PopulateFreedom(List<EntProtoId> variants)
        => _menu?.PopulateFreedom(variants);

    private void CloseMenu()
    {
        if (_menu == null)
            return;

        _menu.OnClose -= CloseMenu;
        _menu.OnSelectStyle -= OnSelectStyle;
        _menu.OnSelectFreedom -= OnSelectFreedom;
        _menu.OnSelectVessel -= OnSelectVessel;

        _menu.Close();
        _menu = null;
    }

    private void OnSelectStyle(ProtoId<PhantomStylePrototype> protoId)
    {
        var player = _playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid;

        var ev = new SelectPhantomStyleEvent(_entityManager.GetNetEntity(player), protoId);
        _entityManager.RaisePredictiveEvent(ev);

        CloseMenu();
    }

    private void OnSelectFreedom(string id)
    {
        var player = _playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid;

        var ev = new SelectPhantomFreedomEvent(_entityManager.GetNetEntity(player), id);
        _entityManager.RaisePredictiveEvent(ev);

        CloseMenu();
    }

    private void OnSelectVessel(NetEntity ent)
    {
        var player = _playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid;

        var ev = new SelectPhantomVesselEvent(_entityManager.GetNetEntity(player), ent);
        _entityManager.RaisePredictiveEvent(ev);

        CloseMenu();
    }
}
