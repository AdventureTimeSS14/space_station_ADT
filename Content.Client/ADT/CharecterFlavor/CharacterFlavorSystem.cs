// Inspired by Nyanotrasen
using Content.Shared.ADT.CharecterFlavor;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.ADT.CharecterFlavor;

public sealed class CharecterFlavorSystem : SharedCharecterFlavorSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SetHeadshotUiMessage>(OnSetHeadshot);
        SubscribeNetworkEvent<HeadshotPreviewEvent>(OnHeadshotPreview);
    }

    private void OnSetHeadshot(SetHeadshotUiMessage args)
    {
        var controller = _ui.GetUIController<CharacterFlavorUiController>();
        controller.SetHeadshot(args.Target, args.Image);
    }

    private void OnHeadshotPreview(HeadshotPreviewEvent ev)
    {
        var controller = _ui.GetUIController<CharacterFlavorUiController>();
        controller.SetPreviewHeadshot(ev.Image);
    }

    /// <summary>
    /// Запросить у сервера предпросмотр хэдшота по URL (используется в лобби).
    /// </summary>
    public void RequestHeadshotPreview(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        RaiseNetworkEvent(new RequestHeadshotPreviewEvent(url));
    }

    protected override void OpenFlavor(EntityUid actor, EntityUid target)
    {
        base.OpenFlavor(actor, target);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (!HasComp<CharacterFlavorComponent>(target))
            return;

        var controller = _ui.GetUIController<CharacterFlavorUiController>();
        controller.OpenMenu(target);
    }
}
