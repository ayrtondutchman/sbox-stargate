using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

public class DhdWorldPanel : WorldPanel
{
	public Dhd Dhd;

	private Label Button1;

	public DhdWorldPanel( Dhd dhd)
	{
		StyleSheet.Load( "/sbox_stargate/ui/DhdWorldPanel.scss" );

		Dhd = dhd;

		Button1 = Add.Label( "1" );

		float width = 2048;
		float height = 2048;

		PanelBounds = new Rect( -width / 2, -height / 2, width, height );

		SceneObject.Flags.BloomLayer = false;
	}

	public override void Tick()
	{
		base.Tick();

		if ( !Dhd.IsValid() )
		{
			Delete();
			return;
		}

		Position = Dhd.Position + Dhd.Rotation.Up * 32;
		var r = Dhd.Rotation;
		r.RotateAroundAxis( r.Forward, 90 );
		Rotation = r;

		var player = Local.Pawn;
		if ( player == null ) return;


	}

}
