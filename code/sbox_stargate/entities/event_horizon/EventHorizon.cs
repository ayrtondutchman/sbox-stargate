using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

public partial class EventHorizon : AnimatedEntity
{
	[Net]
	public Stargate Gate { get; set; } = null;

	[Net]
	public bool IsFullyFormed { get; set; } = false;
	protected Sound WormholeLoop;

	protected Entity CurrentTeleportingEntity;

	// material VARIABLES - probably name this better one day

	// establish material variables
	float minFrame = 0f;
	float maxFrame = 18f;
	float curFrame = 0f;
	bool shouldBeOn = false;
	bool isOn = false;

	bool shouldBeOff = false;
	bool isOff = false;

	// puddle material variables
	float minBrightness = 1f;
	float maxBrightness = 8f;
	float curBrightness = 1f;

	bool shouldEstablish = false;
	bool isEstablished = false;

	bool shouldCollapse = false;
	bool isCollapsed = false;

	TimeSince lastSoundTime = 0;

	[Net]
	private IList<Entity> BufferFront { get; set; } = new();
	[Net]
	private IList<Entity> BufferBack { get; set; } = new();

	public List<Entity> InTransitPlayers { get; set; } = new();

	[Net]
	public int EventHorizonSkinGroup { get; set; } = 0;

	private EventHorizonTrigger FrontTrigger = null;
	private EventHorizonTrigger BackTrigger = null;

	private List<Entity> InTriggerFront { get; set; } = new();
	private List<Entity> InTriggerBack { get; set; } = new();

	public override void Spawn()
	{
		base.Spawn();
		Transmit = TransmitType.Always;

		SetModel( "models/sbox_stargate/event_horizon/event_horizon.vmdl" );
		SkinEstablish();
		SetupPhysicsFromModel( PhysicsMotionType.Static, true );
		PhysicsBody.BodyType = PhysicsBodyType.Static;
		EnableShadowCasting = false;

		Tags.Add( "trigger", "eventhorizon" );

		EnableAllCollisions = false;
		EnableTraceAndQueries = true;
		EnableTouch = true;

		PostSpawn();
	}

	private async void PostSpawn()
	{
		await GameTask.NextPhysicsFrame();

		FrontTrigger = new( this )
		{
			Position = Position + Rotation.Forward * 2,
			Rotation = Rotation,
			Parent = Gate
		};

		BackTrigger = new( this )
		{
			Position = Position - Rotation.Forward * 2,
			Rotation = Rotation.RotateAroundAxis( Vector3.Up, 180 ),
			Parent = Gate
		};
	}

	public virtual void SkinEventHorizon() { SetMaterialGroup( EventHorizonSkinGroup ); }
	public void SkinEstablish() { SetMaterialGroup( 2 ); }

	// SERVER CONTROL

	public async void Establish()
	{
		EstablishClientAnim( To.Everyone ); // clientside animation stuff

		await GameTask.DelaySeconds( 1.5f );
		if ( !this.IsValid() ) return;

		WormholeLoop = Sound.FromEntity( "stargate.event_horizon.loop", this );
	}

	public async void Collapse()
	{
		CollapseClientAnim( To.Everyone ); // clientside animation stuff

		await GameTask.DelaySeconds( 1f );
		if ( !this.IsValid() ) return;

		foreach ( var ent in BufferFront.Concat( BufferBack ) )
		{
			DissolveEntity( ent );
		}

		WormholeLoop.Stop();
	}


	// UTILITY
	public void PlayTeleportSound()
	{
		if ( lastSoundTime > 0.1f ) // delay for playing sounds to avoid constant spam
		{
			lastSoundTime = 0;
			Sound.FromEntity( "stargate.event_horizon.enter", this );
		}
	}

	public bool IsPointBehindEventHorizon( Vector3 point )
	{
		if ( !this.IsValid() ) return false;
		return (point - Position).Dot( Rotation.Forward ) < 0;
	}

	public bool IsEntityBehindEventHorizon( Entity ent )
	{
		if ( !this.IsValid() || !ent.IsValid() ) return false;
		var model = (ent as ModelEntity);
		if ( !model.PhysicsBody.IsValid() ) return false;
		return IsPointBehindEventHorizon( model.PhysicsBody.MassCenter ); // check masscenter instead
	}

	public bool IsPawnBehindEventHorizon( Entity pawn )
	{
		if ( !this.IsValid() || !pawn.IsValid() ) return false;

		var ply = pawn as Player;
		if ( !ply.IsValid() ) return false;

		return (Camera.Position - Position).Dot( Rotation.Forward ) < 0;
	}

	// CLIENT ANIM CONTROL

	[ClientRpc]
	public void TeleportScreenOverlay()
	{
		var hud = Game.RootPanel;
		hud?.AddChild<EventHorizonScreenOverlay>();
	}

	[ClientRpc]
	public void EstablishClientAnim()
	{
		curFrame = minFrame;
		curBrightness = 0;
		shouldBeOn = true;
		shouldBeOff = false;

		SkinEstablish();
	}

	[ClientRpc]
	public void CollapseClientAnim()
	{
		curFrame = maxFrame;
		curBrightness = 1;
		shouldCollapse = true;
		shouldEstablish = false;

		SkinEventHorizon();
	}

	public void ClientAnimLogic()
	{
		SceneObject.Batchable = false;

		if ( shouldBeOn && !isOn )
		{
			curFrame = MathX.Approach( curFrame, maxFrame, Time.Delta * 30 );
			SceneObject.Attributes.Set( "frame", curFrame.FloorToInt() ); // TODO check this

			if ( curFrame == maxFrame )
			{
				isOn = true;
				shouldEstablish = true;
				curBrightness = maxBrightness;
				SkinEventHorizon();

				//Particles.Create( "particles/water_squirt.vpcf", this, "center", true ); // only test, kawoosh particle will be made at some point
			}
		}

		if ( shouldBeOff && !isOff )
		{
			curFrame = MathX.Approach( curFrame, minFrame, Time.Delta * 30 );
			SceneObject.Attributes.Set( "frame", curFrame.FloorToInt() );
			if ( curFrame == minFrame ) isOff = true;
		}

		if ( shouldEstablish && !isEstablished )
		{
			SceneObject.Attributes.Set( "illumbrightness", curBrightness );
			curBrightness = MathX.Approach( curBrightness, minBrightness, Time.Delta * 5 );
			if ( curBrightness == minBrightness ) isEstablished = true;
		}

		if ( shouldCollapse && !isCollapsed )
		{
			SceneObject.Attributes.Set( "illumbrightness", curBrightness );
			curBrightness = MathX.Approach( curBrightness, maxBrightness, Time.Delta * 5 );

			if ( curBrightness == maxBrightness )
			{
				isCollapsed = true;
				shouldBeOff = true;
				curBrightness = minBrightness;
				SkinEstablish();
			}
		}
	}

	public void ClientAlphaRenderLogic()
	{
		// draw the EH at 0.6 alpha when looking at it from behind
		var pawn = Game.LocalPawn;
		if ( pawn.IsValid() ) RenderColor = RenderColor.WithAlpha( IsPawnBehindEventHorizon( pawn ) ? 0.6f : 1f );
	}

	// CLIENT LOGIC
	[Event.Client.Frame]
	public void EventHorizonClientTick()
	{
		ClientAnimLogic();
		ClientAlphaRenderLogic();
	}

	public EventHorizon GetOther()
	{
		if ( !Gate.IsValid() || !Gate.OtherGate.IsValid() )
			return null;

		return Gate.OtherGate.EventHorizon;
	}

	public Tuple<Vector3, Vector3> CalcExitPointAndDir( Vector3 entryPoint, Vector3 entryDir )
	{
		var other = GetOther();

		if ( !other.IsValid() )
			return Tuple.Create( entryPoint, entryDir );

		var newPos = Transform.PointToLocal( entryPoint );
		newPos = newPos.WithY( -newPos.y );
		newPos = other.Transform.PointToWorld( newPos );

		var newDir = Transform.PointToLocal( Position + entryDir );
		newDir = newDir.WithX( -newDir.x ).WithY( -newDir.y );
		newDir = other.Position - other.Transform.PointToWorld( newDir );
		newDir = -newDir;

		return Tuple.Create( newPos, newDir );
	}

	[ClientRpc]
	public void SetPlayerViewAngles( Angles ang )
	{
		(Game.LocalPawn as Player).ViewAngles = ang;
	}

	[ClientRpc]
	private async void PlayWormholeCinematic()
	{
		// TODO: Find a way to call this when the EH is deleted before the cinematic end to not keep the player stuck in this
		var panel = Game.RootPanel.AddChild<WormholeCinematic>();

		await GameTask.DelayRealtimeSeconds( 7.07f );

		panel.Delete( true );
		OnPlayerEndWormhole( NetworkIdent );
	}

	[ConCmd.Server]
	private static void OnPlayerEndWormhole( int netId )
	{
		var eh = FindByIndex<EventHorizon>( netId );
		if ( !eh.IsValid() ) return;

		var pawn = ConsoleSystem.Caller.Pawn as Entity;

		var id = eh.InTransitPlayers.IndexOf( pawn );
		if ( id == -1 ) return;

		eh.InTransitPlayers.RemoveAt( id );
	}

	// TELEPORT
	public void TeleportEntity( Entity ent )
	{
		if ( !Gate.IsValid() || !Gate.OtherGate.IsValid() ) return;

		var otherEH = GetOther();

		if ( !otherEH.IsValid() ) return;

		// at this point, we should be able to teleport just fine

		CurrentTeleportingEntity = ent;
		Gate.OtherGate.EventHorizon.CurrentTeleportingEntity = ent;

		otherEH.PlayTeleportSound(); // other EH plays sound now

		var localVelNorm = Transform.NormalToLocal( ent.Velocity.Normal );
		var otherVelNorm = otherEH.Transform.NormalToWorld( localVelNorm.WithX( -localVelNorm.x ).WithY( -localVelNorm.y ) );

		var scaleDiff = otherEH.Scale / Scale;
		var localPos = Transform.PointToLocal( ent.Position );
		var otherPos = otherEH.Transform.PointToWorld( localPos.WithY( -localPos.y ) * scaleDiff );

		var localRot = Transform.RotationToLocal( ent.Rotation );
		var otherRot = otherEH.Transform.RotationToWorld( localRot.RotateAroundAxis( localRot.Up, 180f ) );

		if ( ent is SandboxPlayer ply )
		{
			TeleportScreenOverlay( To.Single( ply ) );
			var DeltaAngleEH = otherEH.Rotation.Angles() - Rotation.Angles();
			SetPlayerViewAngles( To.Single( ply ), ply.EyeRotation.Angles() + new Angles( 0, DeltaAngleEH.yaw + 180, 0 ) );

			if ( Gate.ShowWormholeCinematic )
			{
				InTransitPlayers.Add( ply );
				PlayWormholeCinematic( To.Single( ply ) );
			}
		}
		else
		{
			ent.Rotation = otherRot;
		}

		var newVel = otherVelNorm * ent.Velocity.Length;

		ent.Velocity = Vector3.Zero;
		ent.Position = otherPos;
		ent.ResetInterpolation();
		ent.Velocity = newVel;

		// after any successful teleport, start autoclose timer if gate should autoclose
		if ( Gate.AutoClose ) Gate.AutoCloseTime = Time.Now + Stargate.AutoCloseTimerDuration + (Gate.ShowWormholeCinematic ? 7 : 0);
	}

	[ClientRpc]
	public void RemoveDeathRagdoll( Player ply )
	{
		ply.Corpse?.Delete();
	}

	public void DissolveEntity( Entity ent )
	{
		if ( ent is SandboxPlayer ply )
		{
			ply.Health = 1;
			var dmg = new DamageInfo();
			dmg.Attacker = Gate;
			dmg.Damage = 100;
			ply.TakeDamage( dmg );

			RemoveDeathRagdoll( To.Single( ply ), ply );

			PlayTeleportSound();
		}
		else
		{
			ent.Delete();
		}
	}

	public void OnEntityEntered( ModelEntity ent, bool fromBack = false )
	{
		if ( !ent.IsValid() )
			return;

		if ( !fromBack && Gate.IsIrisClosed() ) // prevent shit accidentaly touching EH from front if our iris is closed
			return;

		(fromBack ? BufferBack : BufferFront).Add( ent );

		ent.Tags.Add( fromBack ? StargateTags.InBufferBack : StargateTags.InBufferFront );

		//var phys = ent.PhysicsBody;
		//if ( phys.IsValid() )
		//	phys.GravityEnabled = false;

		//var clipPlaneFront = new Plane( Position, Rotation.Forward.Normal );
		//var clipPlaneBack = new Plane( Position, -Rotation.Forward.Normal );

		var alpha = ent.RenderColor.a;
		ent.RenderColor = ent.RenderColor.WithAlpha( alpha.Clamp( 0, 0.99f ) ); // hack to fix MC (doesnt fix it all the times, job for sbox devs)

		SetModelClippingForEntity( To.Everyone, ent, true, fromBack ? ClipPlaneBack : ClipPlaneFront );
	}

	public void OnEntityExited( ModelEntity ent, bool fromBack = false )
	{
		if ( !ent.IsValid() )
			return;

		(fromBack ? BufferBack : BufferFront).Remove( ent );

		ent.Tags.Remove( fromBack ? StargateTags.InBufferBack : StargateTags.InBufferFront );

		//var phys = ent.PhysicsBody;
		//if ( phys.IsValid() )
		//	phys.GravityEnabled = true;

		//var clipPlaneFront = new Plane( Position, Rotation.Forward.Normal );
		//var clipPlaneBack = new Plane( Position, -Rotation.Forward.Normal );

		SetModelClippingForEntity( To.Everyone, ent, false, fromBack ? ClipPlaneBack : ClipPlaneFront );

		if ( ent == CurrentTeleportingEntity )
		{
			CurrentTeleportingEntity = null;
			Gate.OtherGate.EventHorizon.CurrentTeleportingEntity = null;
		}
	}

	public void OnEntityFullyEntered( ModelEntity ent, bool fromBack = false )
	{
		if ( fromBack )
		{
			BufferBack.Remove( ent );
			DissolveEntity( ent );
		}
		else
		{
			BufferFront.Remove( ent );

			async void tpFunc()
			{
				var otherEH = GetOther();
				otherEH.OnEntityEntered( ent, false );
				otherEH.OnEntityTriggerStartTouch( otherEH.FrontTrigger, ent );

				ent.EnableDrawing = false;
				TeleportEntity( ent );

				await GameTask.NextPhysicsFrame(); // cheap trick to avoid seeing the entity on the wrong side of the EH for a few frames
				if ( !this.IsValid() )
					return;

				ent.EnableDrawing = true;
			}

			TeleportLogic( ent, () => tpFunc(), true );
		}

		PlayTeleportSound(); // event horizon always plays sound if something entered it
	}

	public void OnEntityTriggerStartTouch( EventHorizonTrigger trigger, Entity ent )
	{
		if ( trigger == BackTrigger && !InTriggerFront.Contains( ent ) )
		{
			InTriggerBack.Add( ent );
			ent.Tags.Add( StargateTags.BehindGate );
		}

		else if ( trigger == FrontTrigger && !InTriggerBack.Contains( ent ) )
		{
			InTriggerFront.Add( ent );
			ent.Tags.Add( StargateTags.BeforeGate );
		}
	}
	public void OnEntityTriggerEndTouch( EventHorizonTrigger trigger, Entity ent )
	{
		if ( trigger == BackTrigger && InTriggerBack.Contains( ent ) )
		{
			InTriggerBack.Remove( ent );
			ent.Tags.Remove( StargateTags.BehindGate );
		}
		else if ( trigger == FrontTrigger && InTriggerFront.Contains( ent ) )
		{
			InTriggerFront.Remove( ent );
			ent.Tags.Remove( StargateTags.BeforeGate );
		}
	}

	public void TeleportLogic( Entity other, Action teleportFunc, bool skipSideChecks = false )
	{
		if ( Gate.Inbound || !IsFullyFormed ) // if we entered inbound gate from any direction, dissolve
		{
			DissolveEntity( other );
		}
		else // we entered a good gate
		{
			if ( !skipSideChecks && IsEntityBehindEventHorizon( other ) ) // check if we entered from the back and if yes, dissolve
			{
				DissolveEntity( other );
			}
			else // othwerwise we entered from the front, so now decide what happens
			{
				if ( !Gate.IsIrisClosed() ) // try teleporting only if our iris is open
				{
					if ( Gate.OtherGate.IsIrisClosed() ) // if other gate's iris is closed, dissolve
					{
						DissolveEntity( other );
						Gate.OtherGate.Iris.PlayHitSound(); // iris goes boom
					}
					else // otherwise we should be fine for teleportation
					{
						if ( Gate.OtherGate.IsValid() && Gate.OtherGate.EventHorizon.IsValid() )
						{
							teleportFunc();
						}
						else // if the other gate or EH is removed for some reason, dissolve
						{
							DissolveEntity( other );
						}
					}
				}
			}
		}
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );

		if ( !Game.IsServer )
			return;

		if ( other is StargateIris )
			return;

		if ( other == CurrentTeleportingEntity )
			return;

		if ( !IsFullyFormed )
		{
			DissolveEntity( other );
		}

		if ( other is Player ) // players should get teleported instantly on EH touch
		{
			TeleportLogic( other, () => TeleportEntity( other ) );
		}
		else if ( other is ModelEntity modelEnt ) // props get handled differently (aka model clipping)
		{
			OnEntityEntered( modelEnt, IsEntityBehindEventHorizon( modelEnt ) );
		}

	}

	public override void EndTouch( Entity other )
	{
		base.EndTouch( other );

		if ( !other.IsValid() )
			return;

		if ( !Game.IsServer )
			return;

		if ( BufferFront.Contains( other ) ) // entered from front
		{
			if ( IsEntityBehindEventHorizon( other ) ) // entered from front and exited behind the gate (should teleport)
			{
				OnEntityFullyEntered( other as ModelEntity );
			}
			else // entered from front and exited front (should just exit)
			{
				OnEntityExited( other as ModelEntity );
			}
		}

		if ( BufferBack.Contains( other ) ) // entered from back
		{
			if ( IsEntityBehindEventHorizon( other ) ) // entered from back and exited behind the gate (should just exit)
			{
				OnEntityExited( other as ModelEntity, true );
			}
			else // entered from back and exited front (should dissolve)
			{
				OnEntityFullyEntered( other as ModelEntity, true );
			}
		}

		if ( other == CurrentTeleportingEntity && other is Player )
		{
			CurrentTeleportingEntity = null;
			Gate.OtherGate.EventHorizon.CurrentTeleportingEntity = null;
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		WormholeLoop.Stop();

		FrontTrigger?.Delete();
		BackTrigger?.Delete();
	}

	[Event( "server.tick" )]
	public void EventHorizonTick()
	{
		if ( Gate.IsValid() && Scale != Gate.Scale ) Scale = Gate.Scale; // always keep the same scale as gate

		BufferCleanupLogic( BufferFront );
		BufferCleanupLogic( BufferBack );
	}

	public void BufferCleanupLogic( IList<Entity> buffer )
	{
		if ( buffer.Count > 0 )
		{
			for ( var i = buffer.Count - 1; i >= 0; i-- )
			{
				if ( buffer.Count > i )
				{
					var ent = buffer[i];
					if ( !ent.IsValid() )
					{
						if ( buffer.Count > i )
						{
							buffer.RemoveAt( i );
							//Log.Info("cleaned up item from buffer");
						}
					}
				}
			}
		}
	}

	private Plane ClipPlaneFront
	{
		get => new Plane( Position - Camera.Position, Rotation.Forward.Normal );
		
	}

	private Plane ClipPlaneBack
	{
		get => new Plane( Position - Camera.Position, -Rotation.Forward.Normal );
	}

	[ClientRpc]
	public void SetModelClippingForEntity( Entity ent, bool enabled, Plane p )
	{
		var m = ent as ModelEntity;
		if ( !m.IsValid() )
			return;

		//Log.Info( $"Setting MC state of {ent} to {enabled}" );

		var obj = m.SceneObject;
		if ( !obj.IsValid() ) return;

		obj.Batchable = false;
		obj.Attributes.Set( "ClipPlane0", new Vector4( p.Normal, p.Distance ) );
		obj.Attributes.SetCombo( "D_ENABLE_USER_CLIP_PLANE", enabled ); // <-- thanks @MuffinTastic for this line of code
		//obj.Attributes.Set( "translucent", enabled );
	}

	public void UpdateClipPlaneForEntity( Entity ent, Plane p ) // only update plane, not the enabled state
	{
		//Log.Info( $"Updating MC plane of {ent} to {p.Normal}" );
		var m = ent as ModelEntity;
		if ( !m.IsValid() )
			return;

		var obj = m.SceneObject;
		if ( !obj.IsValid() ) return;

		obj.Attributes.Set( "ClipPlane0", new Vector4( p.Normal, p.Distance ) );
	}

	[Event.Client.Frame]
	public void Draw()
	{
		//var clipPlaneFront = ClipPlaneFront;
		//var clipPlaneBack = ClipPlaneBack;

		foreach ( var e in BufferFront )
			UpdateClipPlaneForEntity( e, ClipPlaneFront );

		foreach ( var e in BufferBack )
			UpdateClipPlaneForEntity( e, ClipPlaneBack );
	}
	
}
