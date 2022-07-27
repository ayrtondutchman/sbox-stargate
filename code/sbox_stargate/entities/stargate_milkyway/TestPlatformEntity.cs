using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

[Spawnable]
public partial class TestPlatformEntity : PlatformEntity
{

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
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

}
