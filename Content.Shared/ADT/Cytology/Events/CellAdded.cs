﻿namespace Content.Shared.ADT.Cytology.Events;

public sealed class CellAdded : EntityEventArgs
{
    public readonly NetEntity Entity;
    public readonly Cell Cell;

    public CellAdded(NetEntity entity, Cell cell)
    {
        Entity = entity;
        Cell = cell;
    }
}
