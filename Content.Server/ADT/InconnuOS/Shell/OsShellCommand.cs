using Content.Shared.ADT.InconnuOS.Components;

namespace Content.Server.ADT.InconnuOS.Shell;

public sealed class OsShellContext
{
    public readonly Entity<ADTOperatingSystemComponent> Machine;
    public readonly EntityUid Actor;
    public readonly List<string> Output = new();

    public string WorkingDirectory;

    public OsShellContext(Entity<ADTOperatingSystemComponent> machine, EntityUid actor, string workingDirectory)
    {
        Machine = machine;
        Actor = actor;
        WorkingDirectory = workingDirectory;
    }

    public void Write(string line)
    {
        Output.Add(line);
    }

    public void Write(IEnumerable<string> lines)
    {
        Output.AddRange(lines);
    }

    public void Blank()
    {
        Output.Add(string.Empty);
    }
}

public abstract class OsShellCommand
{
    public abstract string Name { get; }

    public abstract void Execute(OsShellContext context, string[] args);
}
