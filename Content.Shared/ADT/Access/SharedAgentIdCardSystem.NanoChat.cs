using Content.Shared.Access.Components;
using Content.Shared.ADT.NanoChat;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Systems;

public abstract partial class SharedAgentIdCardSystem
{
    [Dependency] private SharedNanoChatSystem _nanoChat = default!;
    [Dependency] private INetManager _net = default!;

    [SubscribeLocalEvent]
    private void OnNumberChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardNumberChangedMessage args)
    {
        if (!TryComp<NanoChatCardComponent>(ent, out var comp))
            return;

        _nanoChat.SetNumber((ent, comp), args.Number);
        Dirty(ent, comp);
        UpdateUi(ent);
    }

    private void CopyNanoChat(EntityUid agent, EntityUid target)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<NanoChatCardComponent>(target, out var targetNanoChat) ||
            !TryComp<NanoChatCardComponent>(agent, out var agentNanoChat))
            return;

        _nanoChat.Clear((agent, agentNanoChat));

        if (_nanoChat.GetNumber((target, targetNanoChat)) is { } number)
            _nanoChat.SetNumber((agent, agentNanoChat), number);

        foreach (var (recipientNumber, recipient) in _nanoChat.GetRecipients((target, targetNanoChat)))
        {
            _nanoChat.SetRecipient((agent, agentNanoChat), recipientNumber, recipient);

            if (_nanoChat.GetMessagesForRecipient((target, targetNanoChat), recipientNumber) is not { } messages)
                continue;

            foreach (var message in messages)
            {
                _nanoChat.AddMessage((agent, agentNanoChat), recipientNumber, message);
            }
        }
    }
}

[Serializable, NetSerializable]
public sealed class AgentIDCardNumberChangedMessage(uint number) : BoundUserInterfaceMessage
{
    public uint Number { get; } = number;
}
