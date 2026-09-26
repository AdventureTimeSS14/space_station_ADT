namespace Content.Server.ADT.Parallax;

[ByRefEvent]
public readonly record struct ADTBiomeChunkLoadedEvent(Box2 Bounds);

[ByRefEvent]
public readonly record struct ADTBiomeChunkUnloadedEvent(Box2 Bounds);
