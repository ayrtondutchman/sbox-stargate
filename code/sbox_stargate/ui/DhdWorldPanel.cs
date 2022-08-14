using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

public class DhdWorldPanel : WorldPanel
{
	public Dhd Dhd;

	private Label Symbol;
	private Vector3 SymbolPosition;

	public DhdWorldPanel( Dhd dhd, string symbol, Vector3 symbolPosition )
	{
		StyleSheet.Load( "/sbox_stargate/ui/DhdWorldPanel.scss" );

		Symbol = Add.Label( symbol );

		float width = 256;
		float height = 64;

		PanelBounds = new Rect( -width / 2, -height / 2, width, height );

		SceneObject.Flags.BloomLayer = false;

		Dhd = dhd;
		SymbolPosition = symbolPosition;
	}

	public override void Tick()
	{
		base.Tick();

		if ( !Dhd.IsValid() )
		{
			Delete();
			return;
		}

		Position = Dhd.Position + Dhd.Rotation.Forward * SymbolPosition.x + Dhd.Rotation.Left * SymbolPosition.y + Dhd.Rotation.Up * SymbolPosition.z;
		Rotation = Dhd.Rotation.RotateAroundAxis(Vector3.Right, 90);
	}

}
