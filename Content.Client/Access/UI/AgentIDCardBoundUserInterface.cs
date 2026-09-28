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

<<<<<<< HEAD
            _window.OnNameChanged += OnNameChanged;
            _window.OnJobChanged += OnJobChanged;
            _window.OnJobIconChanged += OnJobIconChanged;
            // ADT-tweak-start: Наночат
            _window.OnNumberChanged += OnNumberChanged;
        }
        private void OnNumberChanged(uint newNumber)
        {
            SendMessage(new AgentIDCardNumberChangedMessage(newNumber));
        // ADT-tweak-end
        }
=======
        ProtoId<JobIconPrototype> currentIcon = default;
        if (EntMan.TryGetComponent<IdCardComponent>(Owner, out var card))
            currentIcon = card.JobIcon;
>>>>>>> wizards-filtered

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

<<<<<<< HEAD
            _window.SetCurrentName(cast.CurrentName);
            _window.SetCurrentJob(cast.CurrentJob);
            _window.SetAllowedIcons(cast.CurrentJobIconId);
            _window.SetCurrentNumber(cast.CurrentNumber); // ADT-tweak: Наночат
        }
=======
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

    private void OnJobIconChanged(ProtoId<JobIconPrototype> newJobIconId)
    {
        SendPredictedMessage(new AgentIDCardJobIconChangedMessage(newJobIconId));
>>>>>>> wizards-filtered
    }
}
