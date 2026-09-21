using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.ADT.Lavaland.Events;


[ImplicitDataDefinitionForInheritors]
public abstract partial class ADTLavalandEventEffect
{
    public abstract void Run(ADTLavalandEventArgs args);
}

public readonly record struct ADTLavalandEventArgs(
    EntityUid Map,
    ADTSharedLavalandEventSystem Events,
    IRobustRandom Random);

public sealed partial class ADTScatterSpawnEffect : ADTLavalandEventEffect
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField]
    public int MinCount = 1;

    [DataField]
    public int MaxCount = 1;

    [DataField]
    public float MinDistanceFromCenter = 65f;

    [DataField]
    public float MaxRadius = 220f;

    [DataField]
    public float MinSpacing = 16f;

    [DataField]
    public bool AvoidRooms = true;

    [DataField]
    public float RoomClearance = 20f;

    public override void Run(ADTLavalandEventArgs args)
    {
        var count = args.Random.Next(MinCount, MaxCount + 1);

        args.Events.ScatterSpawn(args.Map, Prototype, count, this);
    }
}
