using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.StatusIcon;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Access.UI;

/// <summary>
/// Initializes a <see cref="AgentIDCardWindow"/> and updates it when new server messages are received.
/// </summary>
public sealed class AgentIDCardBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private AgentIDCardWindow? _window;

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent(Owner, out AgentIDCardComponent? agent))
            return;

        _window = this.CreateWindow<AgentIDCardWindow>();

        _window.OnNameChanged += OnNameChanged;
        _window.OnJobChanged += OnJobChanged;
        _window.OnJobIconChanged += OnJobIconChanged;
        // ADT-Tweak-Start
        _window.OnNumberChanged += OnNumberChanged;
        // ADT-Tweak-End

        ProtoId<JobIconPrototype> currentIcon = default;
        if (EntMan.TryGetComponent<IdCardComponent>(Owner, out var card))
            currentIcon = card.JobIcon;

        _window.SetAllowedIcons(agent.IconGroups, currentIcon);
        Update();
    }

    public override void Update()
    {
        base.Update();

        if (_window == null)
            return;

        if (!EntMan.TryGetComponent<IdCardComponent>(Owner, out var card))
            return;

        _window.Update(card);
    }

    private void OnNameChanged(string newName)
    {
        SendPredictedMessage(new AgentIDCardNameChangedMessage(newName));
    }

    private void OnJobChanged(string newJob)
    {
        SendPredictedMessage(new AgentIDCardJobChangedMessage(newJob));
    }

    // ADT-Tweak-Start
    private void OnNumberChanged(uint newNumber)
    {
        SendPredictedMessage(new AgentIDCardNumberChangedMessage(newNumber));
    }
    // ADT-Tweak-End

    private void OnJobIconChanged(ProtoId<JobIconPrototype> newJobIconId)
    {
        SendPredictedMessage(new AgentIDCardJobIconChangedMessage(newJobIconId));
    }

    // ADT-Tweak-Start
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not AgentIDCardBoundUserInterfaceState cast)
            return;

        _window.SetCurrentNumber(cast.CurrentNumber);
    }
    // ADT-Tweak-End
}
