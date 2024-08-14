namespace Content.Shared.Mind;

public sealed class Memory
{
    [ViewVariables(VVAccess.ReadWrite)]
    public string Name { get; set; }

    [ViewVariables(VVAccess.ReadWrite)]
    public string Value { get; set; }

    public Memory(string name, string value)
    {
        Name = name;
        Value = value;
    }
}
