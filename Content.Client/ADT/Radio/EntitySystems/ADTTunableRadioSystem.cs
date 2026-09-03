using Content.Client.ADT.Radio.Ui;
using Content.Shared.ADT.Radio;
using Content.Shared.ADT.Radio.Components;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Radio.EntitySystems;

public sealed class ADTTunableRadioSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTTunableRadioComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnAfterHandleState(Entity<ADTTunableRadioComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi<TunableRadioBoundUserInterface>(ent.Owner, ADTTunableRadioUiKey.Key, out var bui))
            bui.Update(ent);
    }
}
