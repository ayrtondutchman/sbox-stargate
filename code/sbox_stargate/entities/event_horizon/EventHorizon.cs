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

	float lastSoundTime = 0f;

	[Net]
	private List<Entity> BufferFront { get; set; } = new ();
	[Net]
	private List<Entity> BufferBack { get; set; } = new();

	[Net]
	public int EventHorizonSkinGroup { get; set; } = 0;

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
	}

	public virtual void SkinEventHorizon() { SetMaterialGroup( EventHorizonSkinGroup ); }
	public void SkinEstablish() { SetMaterialGroup( 2 ); }

	// SERVER CONTROL

	public async void Establish()
	{
		EstablishClientAnim(To.Everyone); // clientside animation stuff

		await Task.DelaySeconds( 1.5f );
		if ( !this.IsValid() ) return;

		WormholeLoop = Sound.FromEntity( "stargate.event_horizon.loop", this );
	}

	public async void Collapse()
	{
		CollapseClientAnim(To.Everyone); // clientside animation stuff

		await Task.DelaySeconds( 1f );
		if ( !this.IsValid() ) return;

		WormholeLoop.Stop();
	}


	// UTILITY
	public void PlayTeleportSound()
	{
		if ( lastSoundTime + 0.1f < Time.Now ) // delay for playing sounds to avoid constant spam
		{
			lastSoundTime = Time.Now;
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
		return IsPointBehindEventHorizon( ent.Position );
	}

	public bool IsPawnBehindEventHorizon( Entity pawn )
	{
		if ( !this.IsValid() || !pawn.IsValid() ) return false;

		var ply = pawn as Player;
		if ( !ply.IsValid() || ply.CameraMode == null ) return false;

		return (ply.CameraMode.Position - Position).Dot( Rotation.Forward ) < 0;
	}

	// CLIENT ANIM CONTROL

	[ClientRpc]
	public void TeleportScreenOverlay()
	{
		var hud = Local.Hud;
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
		var pawn = Local.Pawn;
		if ( pawn.IsValid() ) RenderColor = RenderColor.WithAlpha(IsPawnBehindEventHorizon(pawn) ? 0.6f : 1f);
	}

	// CLIENT LOGIC
	[Event.Frame]
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

	public Tuple<Vector3, Vector3> CalcExitPointAndDir(Vector3 entryPoint, Vector3 entryDir)
	{
		var other = GetOther();

		if (!other.IsValid())
			return Tuple.Create( entryPoint, entryDir );

		var newPos = Transform.PointToLocal( entryPoint );
		newPos = newPos.WithY( -newPos.y );
		newPos = other.Transform.PointToWorld( newPos );

		var newDir = Transform.PointToLocal( Position + entryDir );
		newDir = newDir.WithX( -newDir.x ).WithY( -newDir.y );
		newDir = other.Position - other.Transform.PointToWorld( newDir );
		newDir = -newDir;

		return Tuple.Create(newPos, newDir);
	}

	// TELEPORT
	public async void TeleportEntity(Entity ent)
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
		var otherRot = otherEH.Transform.RotationToWorld( localRot.RotateAroundAxis(localRot.Up, 180f) );

		if (ent is SandboxPlayer ply)
		{
			TeleportScreenOverlay( To.Single( ply ) );

			var oldController = ply.DevController;
			using ( Prediction.Off() ) ply.DevController = new EventHorizonController();

			var DeltaAngleEH = otherEH.Rotation.Angles() - Rotation.Angles();

			ply.EyeRotation = Rotation.From( ply.EyeRotation.Angles() + new Angles( 0, DeltaAngleEH.yaw + 180, 0 ) );
			ply.Rotation = ply.EyeRotation;

			await GameTask.NextPhysicsFrame();

			using ( Prediction.Off() ) ply.DevController = oldController;
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
		if ( Gate.AutoClose ) Gate.AutoCloseTime = Time.Now + Stargate.AutoCloseTimerDuration;
	}

	[ClientRpc]
	public void RemoveDeathRagdoll(Player ply)
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

			RemoveDeathRagdoll( To.Single( ply ), ply);

			PlayTeleportSound();
		}
		else
		{
			ent.Delete();
		}
	}

	public void OnEntityEntered( ModelEntity ent, bool fromBack=false )
	{
		if ( !ent.IsValid() )
			return;

		if ( !fromBack && Gate.IsIrisClosed() ) // prevent shit accidentaly touching EH from front if our iris is closed
			return;

		(fromBack ? BufferBack : BufferFront ).Add( ent );

		ent.PhysicsBody.GravityEnabled = false;

		var clipPlaneFront = new Plane( Position, Rotation.Forward.Normal );
		var clipPlaneBack = new Plane( Position, -Rotation.Forward.Normal );

		var alpha = ent.RenderColor.a;
		ent.RenderColor = ent.RenderColor.WithAlpha( alpha.Clamp( 0, 0.99f ) ); // hack to fix MC (doesnt fix it all the times, job for sbox devs)

		SetModelClippingForEntity( To.Everyone, ent, true, fromBack ? clipPlaneBack : clipPlaneFront );
	}

	public void OnEntityExited( ModelEntity ent, bool fromBack = false )
	{
		if ( !ent.IsValid() )
			return;

		(fromBack ? BufferBack : BufferFront).Remove( ent );

		ent.PhysicsBody.GravityEnabled = true;

		var clipPlaneFront = new Plane( Position, Rotation.Forward.Normal );
		var clipPlaneBack = new Plane( Position, -Rotation.Forward.Normal );

		SetModelClippingForEntity( To.Everyone, ent, false, fromBack ? clipPlaneBack : clipPlaneFront );

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
				otherEH.BufferFront.Add( ent );

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

		if ( !IsServer )
			return;

		if ( other is StargateIris )
			return;

		if ( other == CurrentTeleportingEntity )
			return;

		// for now only players and props get teleported
		if ( other is Prop ) // props get handled differently (aka model clipping)
		{
			OnEntityEntered( other as ModelEntity, IsEntityBehindEventHorizon( other ) );
		}

		if ( other is Player ) // players should get teleported instantly on EH touch
		{
			TeleportLogic( other, () => TeleportEntity( other ) );
		}
	}

	public override void EndTouch( Entity other )
	{
		base.EndTouch( other );

		if ( !IsServer ) return;

		if (BufferFront.Contains(other)) // entered from front
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

	[ClientRpc]
	public void SetModelClippingForEntity( Entity ent, bool enabled, Plane p )
	{
		var m = ent as ModelEntity;
		if ( !m.IsValid() )
			return;

		//Log.Info( $"Setting MC state of {ent} to {enabled}" );

		var obj = m.SceneObject;
		obj.Attributes.Set( "ClipPlane0", new Vector4( p.Normal, p.Distance ) );
		obj.Attributes.SetCombo( "D_ENABLE_USER_CLIP_PLANE", enabled ); // <-- thanks @MuffinTastic for this line of code
	}

	public void UpdateClipPlaneForEntity( Entity ent, Plane p ) // only update plane, not the enabled state
	{
		//Log.Info( $"Updating MC plane of {ent} to {p.Normal}" );
		var m = ent as ModelEntity;
		if ( !m.IsValid() )
			return;

		var obj = m.SceneObject;
		obj.Attributes.Set( "ClipPlane0", new Vector4( p.Normal, p.Distance ) );
	}

	[Event.Frame]
	public void Draw()
	{
		var clipPlaneFront = new Plane( Position, Rotation.Forward.Normal );
		var clipPlaneBack = new Plane( Position, -Rotation.Forward.Normal );

		foreach ( var e in BufferFront )
			UpdateClipPlaneForEntity( e, clipPlaneFront );

		foreach ( var e in BufferBack )
			UpdateClipPlaneForEntity( e, clipPlaneBack );
	}
	
}
