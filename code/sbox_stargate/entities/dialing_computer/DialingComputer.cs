using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

[Title( "Dialing Computer" ), Category( "Stargate" ), Icon( "chair" ), Spawnable]
public partial class DialingComputer : ModelEntity, IUse
{
	[Net, Change]
	public Stargate Gate { get; set; } = null;

	private DialingComputerHudPanel ComputerPanelHud;
	private DialingComputerWorldPanel ComputerPanelWorld;

	private ComputerProgramDialing Program;

	public override void Spawn()
	{
		base.Spawn();

		Scale = 4;

		Transmit = TransmitType.Always;
		SetModel( "models/editor/ortho.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic, true );
		SetupPhysicsFromOBB( PhysicsMotionType.Dynamic, new Vector3( -5, -5, -5 ), new Vector3( 5, 5, 5 ) );
		PhysicsBody.BodyType = PhysicsBodyType.Static;

		RenderColor = Color.Black;

		Tags.Add( "solid" );
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		Program = new ComputerProgramDialing();
		Program.Computer = this;
		ComputerPanelWorld = new( this, Program );
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );

		if (other is Stargate gate )
		{
			Gate = gate;
		}
	}

	private void OnGateChanged(Stargate oldGate, Stargate newGate)
	{
		Program.Gate = newGate;
	}

	[ClientRpc]
	public void ViewPanelOnHud()
	{
		Program.Parent = null;
		ComputerPanelHud = new DialingComputerHudPanel( this, Program );
		Game.RootPanel.AddChild( ComputerPanelHud );
	}

	[ClientRpc]
	public void ViewPanelOnWorld()
	{
		Program.Parent = null;
		ComputerPanelHud?.Delete( true );
		ComputerPanelWorld.AddChild( Program );
	}

	[ClientRpc]
	public void DeleteBothPanels()
	{
		Program?.Delete( true );
		ComputerPanelHud?.Delete( true );
		ComputerPanelWorld?.Delete( true );
	}

	[ClientRpc]
	private void SwitchPanelViewing()
	{
		if ( !ComputerPanelHud.IsValid() )
			ViewPanelOnHud();
		else
			ViewPanelOnWorld();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		DeleteBothPanels( To.Everyone );
	}

	public bool OnUse( Entity user )
	{
		SwitchPanelViewing( To.Single(user) );
		
		return false;
	}

	public bool IsUsable( Entity user )
	{
		return true;
	}
}
