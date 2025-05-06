﻿using Content.Shared.ADT.Cytology.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Cytology.UI;

[UsedImplicitly]
public sealed class CellSequencerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CellSequencerWindow? _window;

    public CellSequencerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CellSequencerWindow>();

        _window.OnSync += () => SendMessage(new CellSequencerUiSyncMessage());

        _window.OnAdd += cell => SendMessage(new CellSequencerUiAddMessage(cell));
        _window.OnRemove += (cell, remote) => SendMessage(new CellSequencerUiRemoveMessage(cell, remote));
        _window.OnPrint += cell => SendMessage(new CellSequencerUiReplaceMessage(cell));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CellSequencerUiState sequencerUiState)
            return;

        _window?.UpdateState(sequencerUiState);
    }
}
