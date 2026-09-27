namespace Content.Server.ADT.InconnuOS.Shell.Commands;

public sealed class CatCommand : OsShellCommand
{
    public override string Name => "cat";

    private static readonly string[] Art =
    {
        @"        /\_/\",
        @"       ( o.o )",
        @"        > ^ <",
        @"    ____|___|____",
        @"   |             |",
        @"   |  SCHRODIN-  |",
        @"   |   GER ENT.  |",
        @"   |_____________|",
    };

    public override void Execute(OsShellContext context, string[] args)
    {
        context.Write(Art);
        context.Blank();
        context.Write(Loc.GetString("os-terminal-cat"));
    }
}
