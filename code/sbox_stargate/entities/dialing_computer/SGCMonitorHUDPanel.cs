using Sandbox;
using Sandbox.UI;
using System.Threading;

public class SGCMonitorHUDPanel : Panel
{
	private SGCMonitor Monitor;

	public SGCMonitorHUDPanel( SGCMonitor monitor, SGCProgram program )
	{
		Monitor = monitor;

		StyleSheet.Load( "sbox_stargate/entities/dialing_computer/SGCMonitorHUDPanel.scss" );
		var programscreen = Add.Panel( "programscreen" );

		programscreen.AddChild( program );
	}

	public override void Tick()
	{
		base.Tick();

		if ( !Monitor.IsValid() )
		{
			Delete( true );
			return;
		}
	}

	public void ClosePanel()
	{
		Monitor.ViewPanelOnWorld( To.Single( Game.LocalClient ) );
	}

}
