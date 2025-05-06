﻿using Content.Shared.ADT.Cytology.UI;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Cytology.UI;

public sealed class CellularFusionBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CellularFusionWindow? _window;

    public CellularFusionBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CellularFusionWindow>();
        _window.OnSync += () => SendMessage(new CellularFusionUiSyncMessage());
        _window.OnSplice += (cellA, cellB) => SendMessage(new CellularFusionUiSpliceMessage(cellA, cellB));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CellularFusionUiState cellularFusionUiState)
            return;

        _window?.UpdateState(cellularFusionUiState);
    }
}
