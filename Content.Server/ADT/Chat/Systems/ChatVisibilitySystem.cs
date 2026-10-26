using System.Linq;
using Content.Server.Chat.Systems;

namespace Content.Server.ADT.Chat.Systems;

public sealed partial class ChatVisibilitySystem : EntitySystem
{
    [Dependency] private EntityQuery<EyeComponent> _eyeQuery = default!;
    [Dependency] private EntityQuery<VisibilityComponent> _visQuery = default!;
    [Dependency] private EntityQuery<ChatRequiresVisibilityComponent> _reqQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandChat);
    }

    private void OnExpandChat(ExpandICChatRecipientsEvent args)
    {
        if (!_reqQuery.HasComponent(args.Source))
            return;

        if (!_visQuery.TryGetComponent(args.Source, out var vis))
            return;

        foreach (var item in args.Recipients.ToDictionary())
        {
            if (!_eyeQuery.TryGetComponent(item.Key.AttachedEntity, out var eye))
            {
                args.Recipients.Remove(item.Key);
                continue;
            }

            if ((eye.VisibilityMask & vis.Layer) == vis.Layer)
                continue;

            args.Recipients.Remove(item.Key);
        }
    }
}
