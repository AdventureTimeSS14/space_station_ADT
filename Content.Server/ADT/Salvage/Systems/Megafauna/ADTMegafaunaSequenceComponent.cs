namespace Content.Server.ADT.Salvage.Systems;

[RegisterComponent]
public sealed partial class ADTMegafaunaSequenceComponent : Component
{
    [DataField]
    public bool SurvivesDeath;

    [DataField]
    public bool InsertionOrder;

    [ViewVariables]
    public List<ADTMegafaunaStep> Queue = new();
}

public sealed class ADTMegafaunaStep
{
    [ViewVariables]
    public TimeSpan ExecuteAt;

    public Action Execute = default!;
}
