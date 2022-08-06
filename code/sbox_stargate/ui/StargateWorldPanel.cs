using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

public class StargateWorldPanel : WorldPanel
{
	public Stargate Gate;

	private Label Address;
	private Label Group;
	private Label Local;

	public StargateWorldPanel(Stargate gate)
	{
		StyleSheet.Load( "/sbox_stargate/ui/StargateWorldPanel.scss" );

		Gate = gate;

		Address = Add.Label( "Address" );
		Group = Add.Label( "Group" );
		Local = Add.Label( "Local" );

		float width = 2048;
		float height = 2048;

		PanelBounds = new Rect( -width / 2, -height / 2, width, height );

		SceneObject.Flags.BloomLayer = false;
	}

	public override void Tick()
	{
		base.Tick();

		if ( !Gate.IsValid() )
		{
			Delete();
			return;
		}

		Position = Gate.Position + Gate.Rotation.Up * 172;
		Rotation = Gate.Rotation;

		UpdateGateInfo();
	}

	private void UpdateGateInfo()
	{
		Address.Text = $"Address: {Gate.GateAddress}";
		Group.Text = $"Group: {Gate.GateGroup}";
		var localText = Gate.GateLocal ? "Yes" : "No";
		Local.Text = $"Local: {localText}";
	}

}
