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

	[Net]
	public Entity CurrentUser { get; private set; } = null;

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
		Programs.Add( new SGCProgram_Dialing() );
		Programs.Add( new SGCProgram_Screensaver() );
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

	[ClientRpc]
	private void DisableWorldPanel()
	{
		CurrentProgram.Parent = null;
	}

	public bool OnUse( Entity user )
	{
		//SwitchPanelViewing( To.Single( user ) );

		if (CurrentUser.IsValid())
		{
			if (CurrentUser == user)
			{
				ViewPanelOnWorld( To.Single( CurrentUser ) );
				CurrentUser = null;
			}
		}
		else
		{
			CurrentUser = user;
			//DisableWorldPanel( To.Single( CurrentUser ) );
			ViewPanelOnHud( To.Single( CurrentUser ) );
		}

		return false;
	}

	public static bool IsPointBehindPlane( Vector3 point, Vector3 planeOrigin, Vector3 planeNormal )
	{
		return (point - planeOrigin).Dot( planeNormal ) < 0;
	}

	public bool IsWorldPanelInScreen()
	{
		var bounds = WorldPanel.PanelBounds;
		var scaleFactor = (WorldPanel.ActualSize / WorldPanel.RenderSize) / 40.0f;
		var heightScaleModifier = 0.65f;

		var p1 = WorldPanel.Position + WorldPanel.Rotation.Up * (bounds.Height * heightScaleModifier * scaleFactor) + WorldPanel.Rotation.Right * (bounds.Width * scaleFactor);
		var p2 = WorldPanel.Position + WorldPanel.Rotation.Up * (bounds.Height * heightScaleModifier * scaleFactor) + WorldPanel.Rotation.Right * (-bounds.Width * scaleFactor);
		var p3 = WorldPanel.Position + WorldPanel.Rotation.Up * (-bounds.Height * heightScaleModifier * scaleFactor) + WorldPanel.Rotation.Right * (bounds.Width * scaleFactor);
		var p4 = WorldPanel.Position + WorldPanel.Rotation.Up * (-bounds.Height * heightScaleModifier * scaleFactor) + WorldPanel.Rotation.Right * (-bounds.Width * scaleFactor);

		var p1s = p1.ToScreen();
		var p2s = p2.ToScreen();
		var p3s = p3.ToScreen();
		var p4s = p4.ToScreen();

		// check if all points are not in screen, on the same side (so that if we stare at the center and points will be offscreen it won't hide our panel)
		if ((p1s.x < 0 && p2s.x < 0 && p3s.x < 0 && p4s.x < 0) ||
			(p1s.x > 1 && p2s.x > 1 && p3s.x > 1 && p4s.x > 1) ||
			(p1s.y < 0 && p2s.y < 0 && p3s.y < 0 && p4s.y < 0) ||
			(p1s.y > 1 && p2s.y > 1 && p3s.y > 1 && p4s.y > 1))
			{
				return false;
			}

		return true;
	}

	public bool IsWorldPanelBehindScreen()
	{
		var bounds = WorldPanel.PanelBounds;
		var scaleFactor = (WorldPanel.ActualSize / WorldPanel.RenderSize) / 40.0f;
		var heightScaleModifier = 0.65f;

		var p1 = WorldPanel.Position + WorldPanel.Rotation.Up * (bounds.Height * heightScaleModifier * scaleFactor) + WorldPanel.Rotation.Right * (bounds.Width * scaleFactor);
		var p2 = WorldPanel.Position + WorldPanel.Rotation.Up * (bounds.Height * heightScaleModifier * scaleFactor) + WorldPanel.Rotation.Right * (-bounds.Width * scaleFactor);
		var p3 = WorldPanel.Position + WorldPanel.Rotation.Up * (-bounds.Height * heightScaleModifier * scaleFactor) + WorldPanel.Rotation.Right * (bounds.Width * scaleFactor);
		var p4 = WorldPanel.Position + WorldPanel.Rotation.Up * (-bounds.Height * heightScaleModifier * scaleFactor) + WorldPanel.Rotation.Right * (-bounds.Width * scaleFactor);

		var p1s = p1.ToScreen();
		var p2s = p2.ToScreen();
		var p3s = p3.ToScreen();
		var p4s = p4.ToScreen();

		if ( p1s.z < 0 && p2s.z < 0 && p3s.z < 0 && p4s.z < 0 )
			return true;

		return false;
	}

	[Event.Client.Frame]
	private void RenderLogic()
	{
		if ( !WorldPanel.IsValid() )
			return;

		var screenPos = WorldPanel.Position;
		var screenDir = Rotation.Forward;

		bool isWorldPanelBehindScreen = IsWorldPanelBehindScreen();
		bool isPlayerBehindMonitor = IsPointBehindPlane( Camera.Position, screenPos, screenDir );
		bool isPlayerFarAway = Camera.Position.DistanceSquared( screenPos ) > (512 * 512);
		bool isWorldPanelOffScreen = !IsWorldPanelInScreen();

		if ( (isPlayerBehindMonitor || isPlayerFarAway || isWorldPanelOffScreen || isWorldPanelBehindScreen) && CurrentUser != Game.LocalClient && CurrentProgram.Parent == WorldPanel )
		{
			CurrentProgram.Parent = null;
			Log.Info("worldpanel hidden");
		}

		if ( (!isPlayerBehindMonitor && !isPlayerFarAway && !isWorldPanelOffScreen && !isWorldPanelBehindScreen) && CurrentUser != Game.LocalClient && CurrentProgram.Parent == null )
		{
			WorldPanel.AddChild( CurrentProgram );
			Log.Info( "worldpanel unhidden" );
		}
	}

	public bool IsUsable( Entity user )
	{
		return true;
	}

}
