using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;

[Title( "Dialing Computer" ), Category( "Stargate" ), Icon( "chair" ), Spawnable]
public partial class DialingComputer : ModelEntity, IUse
{
	[Net, Change]
	public Stargate Gate { get; set; } = null;

	private DialingComputerHudPanel ComputerPanelHud;
	private DialingComputerWorldPanel ComputerPanelWorld;

	private ComputerProgramDialing Program;

	public static readonly Color Color_SG_Blue = Color.FromBytes( 0, 170, 185 );
	public static readonly Color Color_SG_Yellow = Color.FromBytes( 225, 225, 170 );

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

		Program = new();
		Program.Computer = this;
		ComputerPanelWorld = new( this, Program );
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );

		if ( other is Stargate gate )
		{
			Gate = gate;
		}
	}

	private void OnGateChanged( Stargate oldGate, Stargate newGate )
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
		SwitchPanelViewing( To.Single( user ) );

		return false;
	}

	public bool IsUsable( Entity user )
	{
		return true;
	}

	public float GetSinFromTime()
	{
		var s = (float)Math.Sin( Time.Now );
		return s * s;
	}


	// Events

	[StargateEvent.GateOpening]
	private void GateOpening( Stargate gate )
	{
		if ( gate != Gate ) return;

		Log.Info( $"Stargate {gate} is opening" );
	}

	[StargateEvent.GateOpen]
	private void GateOpen( Stargate gate )
	{
		if ( gate != Gate ) return;

		//Log.Info( $"Stargate {gate} has opened" );
	}

	[StargateEvent.GateClosing]
	private void GateClosing( Stargate gate )
	{
		if ( gate != Gate ) return;

		Log.Info( $"Stargate {gate} is closing" );
	}

	[StargateEvent.GateClosed]
	private void GateClosed( Stargate gate )
	{
		if ( gate != Gate ) return;

		//Log.Info( $"Stargate {gate} has closed" );
	}

	[StargateEvent.ChevronEncoded]
	private void ChevronEncoded( Stargate gate, char sym )
	{
		if ( gate != Gate ) return;

		Log.Info( $"Stargate {gate} has chevron encoded with sym {sym}" );
	}

	[StargateEvent.ChevronLocked]
	private void ChevronLocked( Stargate gate, char sym, bool valid )
	{
		if ( gate != Gate ) return;

		Log.Info( $"Stargate {gate} has { (valid ? "valid" : "invalid") } chevron locked with sym {sym}" );
	}

	[StargateEvent.DHDChevronEncoded]
	private void DHDChevronEncoded( Stargate gate, char sym )
	{
		if ( gate != Gate ) return;

		Log.Info( $"Stargate {gate} has DHD chevron encoded with sym {sym}" );
	}

	[StargateEvent.DHDChevronLocked]
	private void DHDChevronLocked( Stargate gate, char sym, bool valid )
	{
		if ( gate != Gate ) return;

		Log.Info( $"Stargate {gate} has DHD {(valid ? "valid" : "invalid")} chevron locked with sym {sym}" );
	}

	[StargateEvent.DHDChevronUnlocked]
	private void DHDChevronUnlocked( Stargate gate, char sym )
	{
		if ( gate != Gate ) return;

		Log.Info( $"Stargate {gate} has DHD chevron unlocked with sym {sym}" );
	}

	[StargateEvent.RingSpinUp]
	private void RingSpinUp( Stargate gate )
	{
		if ( gate != Gate ) return;

		//Log.Info( $"Stargate {gate} is spinning up ring" );
	}

	[StargateEvent.RingSpinDown]
	private void RingSpinDown( Stargate gate )
	{
		if ( gate != Gate ) return;

		//Log.Info( $"Stargate {gate} is spinning down ring" );
	}

	[StargateEvent.RingStopped]
	private void RingStopped( Stargate gate )
	{
		if ( gate != Gate ) return;

		//Log.Info( $"Stargate {gate} ring stopped" );
	}

	[StargateEvent.ReachedDialingSymbol]
	private void ReachedDialingSymbol( Stargate gate, char sym )
	{
		if ( gate != Gate ) return;

		Log.Info( $"Stargate {gate} has reached dialing symbol {sym}" );
	}

	[StargateEvent.DialBegin]
	private void DialBegin( Stargate gate, string address )
	{
		if ( gate != Gate ) return;

		Log.Info( $"Stargate {gate} started dialing {address}" );
	}

	[StargateEvent.DialAbort]
	private void DialAbort( Stargate gate )
	{
		if ( gate != Gate ) return;

		Log.Info( $"Stargate {gate} aborted dialing" );
	}

	[StargateEvent.InboundBegin]
	private void InboundBegin( Stargate gate )
	{
		if ( gate != Gate ) return;

		Log.Info( $"Stargate {gate} has an incoming wormhole" );
	}

	[StargateEvent.Reset]
	private void Reset( Stargate gate )
	{
		if ( gate != Gate ) return;

		Log.Info( $"Stargate {gate} was reset" );
	}


	[Event.Tick.Server]
	private void Tick()
	{
		if ( !Gate.IsValid() ) return;

		//Log.Info( Gate.DialingAddress );
	}
}
