using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

[Spawnable]
public partial class TestPlatformEntity : PlatformEntity
{
	private float RingCurSpeed = 50f;
	protected float RingMaxSpeed = 50f;
	protected float RingAccelStep = 1f;

	private bool ShouldAcc = false;
	private bool ShouldDecc = false;

	public override void Spawn()
	{
		Transmit = TransmitType.Always;

		SetModel( "models/sbox_stargate/gate_sg1/ring_sg1.vmdl" );
		
		LoopMovement = true;
		MoveDirType = PlatformMoveType.RotatingContinious;
		MoveDirIsLocal = true;
		MoveDir = Rotation.Up.EulerAngles;
		MoveDistance = 360;
		StartsMoving = false;

		base.Spawn();

		EnableAllCollisions = true;
		EnableTraceAndQueries = true;
		PhysicsEnabled = true;

		ShouldAcc = true;
	}

	// TESTING

	[Event.Tick.Server]
	public void Think()
	{
		if (ShouldDecc)
		{
			if (RingCurSpeed > 0)
			{
				RingCurSpeed -= RingAccelStep;
			}
			else
			{
				RingCurSpeed = 0;
				ShouldAcc = true;
				ShouldDecc = false;
			}
		}

		else if ( ShouldAcc )
		{
			if ( RingCurSpeed < RingMaxSpeed )
			{
				RingCurSpeed += RingAccelStep;
			}
			else
			{
				RingCurSpeed = RingMaxSpeed;
				ShouldAcc = false;
				ShouldDecc = true;
			}
		}

		SetSpeed( RingCurSpeed );

		Log.Info( $"Moving={IsMoving}, Speed={Speed}" );
	}

}
