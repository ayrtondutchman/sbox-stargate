using Sandbox;
using Sandbox.UI;

public class DialingComputerHudPanel : Panel
{
	private DialingComputer Computer;

	public DialingComputerHudPanel( DialingComputer computer, Panel program )
	{
		Computer = computer;

		StyleSheet.Load( "sbox_stargate/entities/dialing_computer/DialingComputerHudPanel.scss" );
		var programscreen = Add.Panel( "programscreen" );

		programscreen.AddChild( program );
	}

	public override void Tick()
	{
		base.Tick();

		if ( !Computer.IsValid() )
		{
			Delete( true );
			return;
		}
	}

	public void ClosePanel()
	{
		Computer.ViewPanelOnWorld( To.Single( Game.LocalClient ) );
	}

}
