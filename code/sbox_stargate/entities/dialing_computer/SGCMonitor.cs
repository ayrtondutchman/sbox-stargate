using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;

[Title( "SGC Monitor" ), Category( "Stargate" ), Icon( "chair" ), Spawnable]
public partial class SGCMonitor : ModelEntity, IUse
{
	[Net]
	public SGCComputer Computer { get; private set; } = null;

	private SGCMonitorHUDPanel HUDPanel;
	private SGCMonitorWorldPanel WorldPanel;

	public List<SGCProgram> Programs { get; private set; } = new();
	private SGCProgram CurrentProgram;

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

	private void CreatePrograms()
	{
		Programs.Add( new ComputerProgramDialingV2() );
	}

	[ClientRpc]
	private void UpdatePrograms(SGCComputer computer, SGCMonitor monitor)
	{
		foreach ( var program in Programs )
		{
			program.UpdateProgram( monitor, computer );
		}
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		CreatePrograms();
		UpdatePrograms( Computer, this );

		CurrentProgram = Programs.First();
		WorldPanel = new( this, CurrentProgram );
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );

		if ( other is SGCComputer computer )
		{
			Computer = computer;
			Computer.AddMonitor( this );

			UpdatePrograms( To.Everyone, Computer, this );
		}
	}

	[ClientRpc]
	public void ViewPanelOnHud()
	{
		CurrentProgram.Parent = null;
		HUDPanel = new( this, CurrentProgram );
		Game.RootPanel.AddChild( HUDPanel );
	}

	[ClientRpc]
	public void ViewPanelOnWorld()
	{
		CurrentProgram.Parent = null;
		HUDPanel?.Delete( true );
		WorldPanel.AddChild( CurrentProgram );
	}

	[ClientRpc]
	public void DeleteBothPanels()
	{
		CurrentProgram?.Delete( true );
		HUDPanel?.Delete( true );
		WorldPanel?.Delete( true );
	}

	[ClientRpc]
	private void SwitchPanelViewing()
	{
		if ( !HUDPanel.IsValid() )
			ViewPanelOnHud();
		else
			ViewPanelOnWorld();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if (Computer.IsValid())
			Computer.RemoveMonitor( this );

		DeleteBothPanels( To.Everyone );
	}

	public bool OnUse( Entity user )
	{
		SwitchPanelViewing( To.Single( user ) );

		return false;
	}

	public bool IsUsable( Entity user )
	{
		return true;
	}

}
