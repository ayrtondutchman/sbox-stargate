using Sandbox;
using Sandbox.Physics;
using System;
using System.Linq;

[Library( "physgun" )]
public partial class PhysGun : Carriable
{
	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

	protected PhysicsBody holdBody;
	protected PhysicsBody velBody;
	protected FixedJoint holdJoint;
	protected FixedJoint velJoint;

	protected PhysicsBody heldBody;
	protected Vector3 heldPos;
	protected Rotation heldRot;

	protected float holdDistance;
	protected bool grabbing;

	protected virtual float MinTargetDistance => 0.0f;
	protected virtual float MaxTargetDistance => 10000.0f;
	protected virtual float LinearFrequency => 20.0f;
	protected virtual float LinearDampingRatio => 1.0f;
	protected virtual float AngularFrequency => 20.0f;
	protected virtual float AngularDampingRatio => 1.0f;
	protected virtual float TargetDistanceSpeed => 50.0f;
	protected virtual float RotateSpeed => 0.2f;
	protected virtual float RotateSnapAt => 45.0f;

	[Net] public bool BeamActive { get; set; }
	[Net] public Entity GrabbedEntity { get; set; }
	[Net] public int GrabbedBone { get; set; }
	[Net] public Vector3 GrabbedPos { get; set; }

	public PhysicsBody HeldBody => heldBody;

	public override void Spawn()
	{
		base.Spawn();

		Tags.Add( "weapon" );
		SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );
	}

	public override void Simulate( IClient client )
	{
		if ( Owner is not Player owner ) return;

		var eyePos = owner.EyePosition;
		var eyeDir = owner.EyeRotation.Forward;
		var eyeRot = Rotation.From( new Angles( 0.0f, owner.EyeRotation.Angles().yaw, 0.0f ) );

		if ( Input.Pressed( InputButton.PrimaryAttack ) )
		{
			(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );

			if ( !grabbing )
				grabbing = true;
		}

		bool grabEnabled = grabbing && Input.Down( InputButton.PrimaryAttack );
		bool wantsToFreeze = Input.Pressed( InputButton.SecondaryAttack );

		if ( GrabbedEntity.IsValid() && wantsToFreeze )
		{
			(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );
		}

		BeamActive = grabEnabled;

		if ( Game.IsServer )
		{
			using ( Prediction.Off() )
			{
				if ( !holdBody.IsValid() )
					return;

				if ( grabEnabled )
				{
					if ( heldBody.IsValid() )
					{
						UpdateGrab( eyePos, eyeRot, eyeDir, wantsToFreeze );
					}
					else
					{
						TryStartGrab( owner, eyePos, eyeRot, eyeDir );
					}
				}
				else if ( grabbing )
				{
					GrabEnd();
				}

				if ( !grabbing && Input.Pressed( InputButton.Reload ) )
				{
					TryUnfreezeAll( owner, eyePos, eyeRot, eyeDir );
				}
			}
		}

		if ( BeamActive )
		{
			Input.MouseWheel = 0;
		}
	}

	private static bool IsBodyGrabbed( PhysicsBody body )
	{
		// There for sure is a better way to deal with this
		if ( All.OfType<PhysGun>().Any( x => x?.HeldBody?.PhysicsGroup == body?.PhysicsGroup ) ) return true;
		if ( All.OfType<GravGun>().Any( x => x?.HeldBody?.PhysicsGroup == body?.PhysicsGroup ) ) return true;

		return false;
	}

	private void TryUnfreezeAll( Player owner, Vector3 eyePos, Rotation eyeRot, Vector3 eyeDir )
	{
		var tr = Trace.Ray( eyePos, eyePos + eyeDir * MaxTargetDistance )
			.UseHitboxes()
			.Ignore( this )
			.Run();

		if ( !tr.Hit || !tr.Entity.IsValid() || tr.Entity.IsWorld ) return;

		var rootEnt = tr.Entity.Root;
		if ( !rootEnt.IsValid() ) return;

		var physicsGroup = rootEnt.PhysicsGroup;
		if ( physicsGroup == null ) return;

		bool unfrozen = false;

		for ( int i = 0; i < physicsGroup.BodyCount; ++i )
		{
			var body = physicsGroup.GetBody( i );
			if ( !body.IsValid() ) continue;

			if ( body.BodyType == PhysicsBodyType.Static )
			{
				body.BodyType = PhysicsBodyType.Dynamic;
				unfrozen = true;
			}
		}

		if ( unfrozen )
		{
			var freezeEffect = Particles.Create( "particles/physgun_freeze.vpcf" );
			freezeEffect.SetPosition( 0, tr.EndPosition );
		}
	}

	private void TryStartGrab( Player owner, Vector3 eyePos, Rotation eyeRot, Vector3 eyeDir )
	{
		var startPos = eyePos;
		var endPos = eyePos + eyeDir * MaxTargetDistance;

		// pre check if we will be hitting an entity thats in the gate buffer
		var trCheck = Trace.Ray( startPos, endPos )
			.UseHitboxes()
			.WithTag( "solid" )
			.WithAnyTags( StargateTags.InBufferFront, StargateTags.InBufferBack )
			.Ignore( this )
			.Run();

		// real trace for getting the target entity
		var trace = Trace.Ray( startPos, endPos )
			.UseHitboxes()
			.WithAnyTags( "solid", StargateTags.EventHorizon, "debris", "item" )
			.Ignore( this );

		// if we hit buffer ent, check on which side we hit it and exclude from real trace if necessary
		if (trCheck.Hit)
		{
			var trEnd = trCheck.EndPosition;
			var closestGate = Stargate.FindClosestGate( trEnd );
			var isEndBehind = Stargate.IsPointBehindEventHorizon( trEnd, closestGate );

			trace = trace.WithoutTags( isEndBehind ? StargateTags.InBufferFront : StargateTags.InBufferBack );
		}

		// run the real trace
		var tr = trace.Run();

		if ( !tr.Hit || !tr.Entity.IsValid() || tr.Entity.IsWorld || tr.StartedSolid ) return;

		// if we hit EH, dont grab anything
		if ( tr.Entity is EventHorizon ) return;

		var rootEnt = tr.Entity.Root;
		var body = tr.Body;

		if ( !body.IsValid() || tr.Entity.Parent.IsValid() )
		{
			if ( rootEnt.IsValid() && rootEnt.PhysicsGroup != null )
			{
				body = (rootEnt.PhysicsGroup.BodyCount > 0 ? rootEnt.PhysicsGroup.GetBody( 0 ) : null);
			}
		}

		if ( !body.IsValid() )
			return;

		//
		// Don't move keyframed, unless it's a player
		//
		if ( body.BodyType == PhysicsBodyType.Keyframed && rootEnt is not Player )
			return;

		// Dont allow touching GateSpawner entities
		if ( GateSpawner.GateSpawnerEntites.Contains( body.GetEntity() ) )
			return;

		// Unfreeze
		if ( body.BodyType == PhysicsBodyType.Static )
		{
			body.BodyType = PhysicsBodyType.Dynamic;
		}

		if ( IsBodyGrabbed( body ) )
			return;

		GrabInit( body, eyePos, tr.EndPosition, eyeRot );

		GrabbedEntity = rootEnt;
		GrabbedPos = body.Transform.PointToLocal( tr.EndPosition );
		GrabbedBone = body.GroupIndex;

		Client?.Pvs.Add( GrabbedEntity );
	}

	private void UpdateGrab( Vector3 eyePos, Rotation eyeRot, Vector3 eyeDir, bool wantsToFreeze )
	{
		if ( wantsToFreeze )
		{
			if ( heldBody.BodyType == PhysicsBodyType.Dynamic )
			{
				heldBody.BodyType = PhysicsBodyType.Static;
			}

			if ( GrabbedEntity.IsValid() )
			{
				var freezeEffect = Particles.Create( "particles/physgun_freeze.vpcf" );
				freezeEffect.SetPosition( 0, heldBody.Transform.PointToWorld( GrabbedPos ) );
			}

			GrabEnd();
			return;
		}

		MoveTargetDistance( Input.MouseWheel * TargetDistanceSpeed );

		bool rotating = Input.Down( InputButton.Use );
		bool snapping = false;

		if ( rotating )
		{
			DoRotate( eyeRot, Input.MouseDelta * RotateSpeed );
			snapping = Input.Down( InputButton.Run );
		}

		GrabMove( eyePos, eyeDir, eyeRot, snapping );
	}

	private void Activate()
	{
		if ( !Game.IsServer )
			return;

		if ( !holdBody.IsValid() )
		{
			holdBody = new PhysicsBody( Game.PhysicsWorld )
			{
				BodyType = PhysicsBodyType.Keyframed
			};
		}

		if ( !velBody.IsValid() )
		{
			velBody = new PhysicsBody( Game.PhysicsWorld )
			{
				BodyType = PhysicsBodyType.Dynamic,
				AutoSleep = false
			};
		}
	}

	private void Deactivate()
	{
		if ( Game.IsServer )
		{
			GrabEnd();

			holdBody?.Remove();
			holdBody = null;

			velBody?.Remove();
			velBody = null;
		}

		KillEffects();
	}

	public override void ActiveStart( Entity ent )
	{
		base.ActiveStart( ent );

		Activate();
	}

	public override void ActiveEnd( Entity ent, bool dropped )
	{
		base.ActiveEnd( ent, dropped );

		Deactivate();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		Deactivate();
	}

	public override void OnCarryDrop( Entity dropper )
	{
	}

	private void GrabInit( PhysicsBody body, Vector3 startPos, Vector3 grabPos, Rotation rot )
	{
		if ( !body.IsValid() )
			return;

		GrabEnd();

		grabbing = true;
		heldBody = body;
		holdDistance = Vector3.DistanceBetween( startPos, grabPos );
		holdDistance = holdDistance.Clamp( MinTargetDistance, MaxTargetDistance );

		heldRot = rot.Inverse * heldBody.Rotation;

		holdBody.Position = grabPos;
		holdBody.Rotation = heldBody.Rotation;

		velBody.Position = grabPos;
		velBody.Rotation = heldBody.Rotation;

		heldBody.Sleeping = false;
		heldBody.AutoSleep = false;

		holdJoint = PhysicsJoint.CreateFixed( holdBody, heldBody.WorldPoint( grabPos ) );
		holdJoint.SpringLinear = new PhysicsSpring( LinearFrequency, LinearDampingRatio );
		holdJoint.SpringAngular = new PhysicsSpring( AngularFrequency, AngularDampingRatio );

		velJoint = PhysicsJoint.CreateFixed( holdBody, velBody );
		velJoint.SpringLinear = new PhysicsSpring( LinearFrequency, LinearDampingRatio );
		velJoint.SpringAngular = new PhysicsSpring( AngularFrequency, AngularDampingRatio );

	}

	private void GrabEnd()
	{
		holdJoint?.Remove();
		holdJoint = null;

		velJoint?.Remove();
		velJoint = null;

		if ( heldBody.IsValid() )
		{
			heldBody.AutoSleep = true;
		}

		Client?.Pvs.Remove( GrabbedEntity );

		heldBody = null;
		GrabbedEntity = null;
		grabbing = false;
	}

	private void GrabMove( Vector3 startPos, Vector3 dir, Rotation rot, bool snapAngles )
	{
		if ( !heldBody.IsValid() )
			return;

		holdBody.Position = startPos + dir * holdDistance;

		if ( GrabbedEntity is Player player )
		{
			player.Velocity = velBody.Velocity;
			player.Position = holdBody.Position - heldPos;

			var controller = player.GetActiveController();
			if ( controller != null )
			{
				controller.Velocity = velBody.Velocity;
			}

			return;
		}

		holdBody.Rotation = rot * heldRot;

		if ( snapAngles )
		{
			var angles = holdBody.Rotation.Angles();

			holdBody.Rotation = Rotation.From(
				MathF.Round( angles.pitch / RotateSnapAt ) * RotateSnapAt,
				MathF.Round( angles.yaw / RotateSnapAt ) * RotateSnapAt,
				MathF.Round( angles.roll / RotateSnapAt ) * RotateSnapAt
			);
		}
	}

	private void MoveTargetDistance( float distance )
	{
		holdDistance += distance;
		holdDistance = holdDistance.Clamp( MinTargetDistance, MaxTargetDistance );
	}

	protected virtual void DoRotate( Rotation eye, Vector3 input )
	{
		var localRot = eye;
		localRot *= Rotation.FromAxis( Vector3.Up, input.x );
		localRot *= Rotation.FromAxis( Vector3.Right, input.y );
		localRot = eye.Inverse * localRot;

		heldRot = localRot * heldRot;
	}

	public override void BuildInput()
	{
		if ( !Input.Down( InputButton.Use ) || !Input.Down( InputButton.PrimaryAttack ) || !GrabbedEntity.IsValid() )
		{
			return;
		}

		//
		// Lock view angles
		//
		if ( Owner is Player ply )
		{
			ply.ViewAngles = ply.OriginalViewAngles;
		}
	}

	public override bool IsUsable( Entity user )
	{
		return Owner == null || HeldBody.IsValid();
	}
}
