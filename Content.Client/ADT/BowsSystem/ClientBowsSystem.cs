using Content.Shared.Input;
using Content.Shared.Interaction.Events;
using Robust.Client.Input;
using Robust.Shared.Input;
using Content.Shared.ADT.BowsSystem;

namespace Content.Client.ADT.BowsSystem;
public sealed class ClientBowsSystem : EntitySystem
{
    [Dependency] private readonly IInputManager _input = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpendedBowsComponent, UseInHandEvent>(OnUseInHand);
    }

    public bool OnUseInHand(Entity<ExpendedBowsComponent> bow, ref UseInHandEvent args)
    {
        if (_input.TryGetKeyBinding(ContentKeyFunctions.UseItemInHand))
            return bow.Comp.IsHoldingKey=true;
        else
            return bow.Comp.IsHoldingKey=false;
    }
}