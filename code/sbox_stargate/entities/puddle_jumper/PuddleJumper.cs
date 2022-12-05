using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

[Title( "Puddle Jumper" ), Category( "Stargate" ), Icon( "chair" )]
public partial class PuddleJumper : Prop, IUse
{
	[Net] public Vector3 SpawnOffset { get; private set; } = new( 0, 0, 65 );

	public virtual float altitudeAcceleration => 2000;
	public virtual float movementAcceleration => 5000;
	public virtual float yawSpeed => 150;
	public virtual float uprightSpeed => 5000;
	public virtual float uprightDot => 0.5f;
	public virtual float leanWeight => 0.5f;
	public virtual float leanMaxVelocity => 1000;

	private TimeSince timeSinceDriverLeft;

	private struct DroneInputState
	{
		public Vector3 movement;
		public float throttle;
		public float pitch;
		public float yaw;

		public void Reset()
		{
			movement = Vector3.Zero;
			pitch = 0;
			yaw = 0;
		}
	}

	private DroneInputState currentInput;

	[Net] public Player Driver { get; private set; }

	public override void Spawn()
	{
		base.Spawn();

		Components.Create<PuddleJumperCamera>();

		SetModel( "models/sbox_stargate/puddle_jumper/puddle_jumper.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
	}

	[Event.Physics.PostStep]
	protected void ApplyForces()
	{
		if ( !PhysicsBody.IsValid() )
		{
			return;
		}

		var body = PhysicsBody;
		var transform = Transform;

		body.LinearDrag = 1.0f;
		body.AngularDrag = 1.0f;
		body.LinearDamping = 4.0f;
		body.AngularDamping = 4.0f;

		var yawRot = Rotation.From( new Angles( 0, Rotation.Angles().yaw, 0 ) );
		var worldMovement = yawRot * currentInput.movement;
		var velocityDirection = body.Velocity.WithZ( 0 );
		var velocityMagnitude = velocityDirection.Length;
		velocityDirection = velocityDirection.Normal;

		var velocityScale = (velocityMagnitude / leanMaxVelocity).Clamp( 0, 1 );
		var leanDirection = worldMovement.LengthSquared == 0.0f
			? -velocityScale * velocityDirection
			: worldMovement;

		var targetUp = (Vector3.Up + leanDirection * leanWeight * velocityScale).Normal;
		var currentUp = transform.NormalToWorld( Vector3.Up );
		var alignment = Math.Max( Vector3.Dot( targetUp, currentUp ), 0 );

		bool hasCollision = false;
		bool isGrounded = false;

		if ( !hasCollision || isGrounded )
		{
			var hoverForce = isGrounded && currentInput.throttle <= 0 ? Vector3.Zero : -1 * transform.NormalToWorld( Vector3.Up ) * -800.0f;
			var movementForce = isGrounded ? Vector3.Zero : worldMovement * movementAcceleration;
			var altitudeForce = transform.NormalToWorld( Vector3.Up ) * currentInput.throttle * altitudeAcceleration;
			var totalForce = hoverForce + movementForce + altitudeForce;
			body.ApplyForce( (totalForce * alignment) * body.Mass );
		}

		if ( !hasCollision && !isGrounded )
		{
			var spinTorque = Transform.NormalToWorld( new Vector3( 0, 0, currentInput.yaw * yawSpeed ) );
			var uprightTorque = Vector3.Cross( currentUp, targetUp ) * uprightSpeed;
			var uprightAlignment = alignment < uprightDot ? 0 : alignment;
			var totalTorque = spinTorque * alignment + uprightTorque * uprightAlignment;
			body.ApplyTorque( (totalTorque * alignment) * body.Mass );
		}
	}

	public override void Simulate( Client owner )
	{
		if ( owner == null ) return;
		if ( !IsServer ) return;

		SimulateDriver( owner );

		using ( Prediction.Off() )
		{
			currentInput.Reset();
			var x = (Input.Down( InputButton.Forward ) ? -1 : 0) + (Input.Down( InputButton.Back ) ? 1 : 0);
			var y = (Input.Down( InputButton.Right ) ? 1 : 0) + (Input.Down( InputButton.Left ) ? -1 : 0);
			currentInput.movement = new Vector3( -x, -y, 0 ).Normal;
			currentInput.throttle = (Input.Down( InputButton.Jump ) ? 1 : 0) + (Input.Down( InputButton.Duck ) ? -1 : 0);
			currentInput.yaw = -Input.MouseDelta.x;
		}
	}

	void SimulateDriver( Client client )
	{
		if ( !Driver.IsValid() ) return;

		if ( IsServer && Input.Pressed( InputButton.Use ) )
		{
			RemoveDriver( Driver as SandboxPlayer );
			return;
		}

		// TODO - at this point the driver isn't actually predicted
		// because they're not our pawn. We need a pawn stack or some shit.

		//driver.Simulate( client );
		//Driver.ActiveChild?.Simulate( client );

		Driver.SetAnimParameter( "b_grounded", true );
		Driver.SetAnimParameter( "b_noclip", false );
		Driver.SetAnimParameter( "sit", 1 );

		var viewAngles = Driver.ViewAngles.ToRotation();
		var aimRotation = viewAngles.Clamp( Driver.Rotation, 90 );

		var aimPos = Driver.EyePosition + aimRotation.Forward * 200;
		var localPos = new Transform( Driver.EyePosition, Driver.Rotation ).PointToLocal( aimPos );

		Driver.SetAnimParameter( "aim_eyes", localPos );
		Driver.SetAnimParameter( "aim_head", localPos );
		Driver.SetAnimParameter( "aim_body", localPos );

		if ( Driver.ActiveChild is BaseCarriable carry )
		{
			//carry.SimulateAnimator( null );
		}
		else
		{
			Driver.SetAnimParameter( "holdtype", 0 );
			Driver.SetAnimParameter( "aim_body_weight", 0.5f );
		}
	}

	public void ResetInput()
	{
		currentInput.Reset();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if ( Driver is SandboxPlayer player )
		{
			RemoveDriver( player );
		}
	}

	private void RemoveDriver( SandboxPlayer player )
	{
		Driver?.SetAnimParameter( "sit", 0 );

		Driver = null;
		timeSinceDriverLeft = 0;

		ResetInput();

		if ( !player.IsValid() )
			return;

		player.Parent = null;

		if ( player.PhysicsBody.IsValid() )
		{
			player.PhysicsBody.Enabled = true;
			player.PhysicsBody.Position = player.Position;
		}

		player.Client.Pawn = player;
	}

	public bool OnUse( Entity user )
	{
		if ( user is SandboxPlayer player && timeSinceDriverLeft > 1.0f )
		{
			player.Parent = this;
			player.LocalPosition = Vector3.Up * 10;
			player.LocalRotation = Rotation.Identity;
			player.LocalScale = 1;
			player.PhysicsBody.Enabled = false;

			Driver = player;

			player.Client.Pawn = this;
		}

		return false;
	}

	public bool IsUsable( Entity user )
	{
		return Driver == null;
	}
}
