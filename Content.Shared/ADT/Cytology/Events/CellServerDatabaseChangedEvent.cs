using Content.Shared.ADT.Cytology.Components.Server;

namespace Content.Shared.ADT.Cytology.Events;

public sealed class CellServerDatabaseChangedEvent : EntityEventArgs
{
    public readonly Entity<CellServerComponent> Server;
    public readonly Entity<CellClientComponent> Client;

    public CellServerDatabaseChangedEvent(Entity<CellServerComponent> server, Entity<CellClientComponent> client)
    {
        Server = server;
        Client = client;
    }
}
