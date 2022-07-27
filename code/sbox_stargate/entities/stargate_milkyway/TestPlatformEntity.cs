using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

public partial class TestPlatformEntity : PlatformEntity
{
	// ring variables

	

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
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

}
