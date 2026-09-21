using Robust.Client.Console;

namespace Content.Client.ADT.UserInterface.Buttons;

[Virtual]
public class ADTCommandButton : ADTLobbyTextButton
{
    public string? Command { get; set; }

    public ADTCommandButton()
    {
        OnPressed += Execute;
    }

    private bool CanPress()
    {
        return string.IsNullOrEmpty(Command) ||
               IoCManager.Resolve<IClientConGroupController>().CanCommand(Command.Split(' ')[0]);
    }

    protected override void EnteredTree()
    {
        if (!CanPress())
        {
            Visible = false;
        }
    }

    protected virtual void Execute(ButtonEventArgs obj)
    {
        if (!string.IsNullOrEmpty(Command))
            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(Command);
    }
}
