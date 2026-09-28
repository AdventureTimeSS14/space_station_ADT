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

    private void OnJobIconChanged(ProtoId<JobIconPrototype> newJobIconId)
    {
<<<<<<< ours
        private AgentIDCardWindow? _window;

        public AgentIDCardBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<AgentIDCardWindow>();

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

        private void OnNameChanged(string newName)
        {
            SendMessage(new AgentIDCardNameChangedMessage(newName));
        }

        private void OnJobChanged(string newJob)
        {
            SendMessage(new AgentIDCardJobChangedMessage(newJob));
        }

        public void OnJobIconChanged(ProtoId<JobIconPrototype> newJobIconId)
        {
            SendMessage(new AgentIDCardJobIconChangedMessage(newJobIconId));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not AgentIDCardBoundUserInterfaceState cast)
                return;

            _window.SetCurrentName(cast.CurrentName);
            _window.SetCurrentJob(cast.CurrentJob);
            _window.SetAllowedIcons(cast.CurrentJobIconId);
            _window.SetCurrentNumber(cast.CurrentNumber); // ADT-tweak: Наночат
        }
||||||| base
        private AgentIDCardWindow? _window;

        public AgentIDCardBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<AgentIDCardWindow>();

            _window.OnNameChanged += OnNameChanged;
            _window.OnJobChanged += OnJobChanged;
            _window.OnJobIconChanged += OnJobIconChanged;
        }

        private void OnNameChanged(string newName)
        {
            SendMessage(new AgentIDCardNameChangedMessage(newName));
        }

        private void OnJobChanged(string newJob)
        {
            SendMessage(new AgentIDCardJobChangedMessage(newJob));
        }

        public void OnJobIconChanged(ProtoId<JobIconPrototype> newJobIconId)
        {
            SendMessage(new AgentIDCardJobIconChangedMessage(newJobIconId));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not AgentIDCardBoundUserInterfaceState cast)
                return;

            _window.SetCurrentName(cast.CurrentName);
            _window.SetCurrentJob(cast.CurrentJob);
            _window.SetAllowedIcons(cast.CurrentJobIconId);
        }
=======
        SendPredictedMessage(new AgentIDCardJobIconChangedMessage(newJobIconId));
>>>>>>> theirs
    }
}
