using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Emoting;

public abstract class SharedAnimatedEmotesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimatedEmotesComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(Entity<AnimatedEmotesComponent> ent, ref ComponentGetState args)
    {
        args.State = new AnimatedEmotesComponentState(ent.Comp.Emote, ent.Comp.EmoteTime);
    }

    public void SetEmote(Entity<AnimatedEmotesComponent> ent, ProtoId<EmotePrototype> emote)
    {
        if (_net.IsClient)
            return;

        ent.Comp.Emote = emote;
        ent.Comp.EmoteTime = _timing.CurTime;
        Dirty(ent);
    }
}
