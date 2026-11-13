using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI;

public sealed class InconnuOsWindow : DefaultWindow
{
    private readonly OsRoot _root;

    public event Action<BoundUserInterfaceMessage>? OnMessage;
    public event Action? OnSessionReset;

    public InconnuOsWindow(
        IPrototypeManager prototypes,
        IResourceCache cache,
        IGameTiming timing,
        IEntityManager entities,
        OsAppRegistry registry,
        ADTOsBuiState state,
        OsSession? session)
    {
        MinSize = new Vector2(760f, 520f);
        SetSize = new Vector2(1040f, 700f);

        _root = new OsRoot(prototypes, cache, timing, entities, registry, state, session)
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        _root.OnMessage += message => OnMessage?.Invoke(message);
        _root.OnSessionReset += () => OnSessionReset?.Invoke();

        Contents.AddChild(_root);

        SetTitle(state);
    }

    public void SetState(ADTOsBuiState state)
    {
        SetTitle(state);

        _root.SetState(state);
    }

    public void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        _root.ReceiveMessage(message);
    }

    public OsSession SaveSession()
    {
        return _root.SaveSession();
    }

    private void SetTitle(ADTOsBuiState state)
    {
        Title = $"{OsBrand.Name} - {state.MachineName}";
    }
}
