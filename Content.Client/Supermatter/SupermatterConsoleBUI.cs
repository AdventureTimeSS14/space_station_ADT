using Robust.Client.UserInterface;

namespace Content.Client.Supermatter;

public sealed class SupermatterConsoleBoundUserInterface : BoundUserInterface
{
	private SuppermatterControlWindow? _window;

	public SupermatterControlBoundUserInterface(NetEntity owner, Enum uiKey) : base(owner, uiKey)
	{
	}

	protected override void Open()
	{
	    base.Open();

	    _window = this.CreateWindow<SuppermatterControlWindow>();
	}

	protected override void UpdateState(BoundUserInterfaceState state)
	{
		base.UpdateState(state);
		if (_window == null || state is not EnergyCoreConsoleUpdateState cast) return;
		_window.UpdatePercents(cast.Procents);
		_window.UpdateGases(cast.Gases);
	}
	
	protected override void Dispose(bool disposing)
	{
	    base.Dispose(disposing);

	    if (!disposing)
	        return;

	    _window?.Dispose();
	}
}	
