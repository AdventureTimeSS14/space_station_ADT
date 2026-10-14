namespace Content.Server.ADT.Salvage.Systems;

[RegisterComponent]
public sealed partial class ADTMegafaunaSequenceComponent : Component
{
    [ViewVariables]
    public List<ADTMegafaunaStep> Queue = new();
}

public sealed class ADTMegafaunaStep
{
    [ViewVariables]
    public TimeSpan ExecuteAt;

    public Action Execute = default!;
}
