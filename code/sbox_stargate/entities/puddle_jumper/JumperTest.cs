using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

[Title( "Test Jumper" ), Category( "Stargate" ), Icon( "chair" )]
public partial class JumperTest : Prop, IUse
{
	[Net] public Vector3 SpawnOffset { get; private set; } = new( 0, 0, 65 );

	private Transform TargetTransform;
	private bool On = false;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/sbox_stargate/puddle_jumper/puddle_jumper.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );

		//PhysicsBody.GravityEnabled = false;
		//PhysicsBody.UseController = true;

		PostSpawn();
	}

	public async void PostSpawn()
	{
		await Task.Delay(500);

		PhysicsBody.UseController = true;

		TargetTransform = Transform.WithPosition(Position + Rotation.Up * 64);
		On = true;
	}

	[Event.Physics.PreStep]
	public void PhysicsSimulate()
	{
		if ( !On )
			return;

		var phys = PhysicsBody;
		if ( !phys.IsValid() )
			return;

		PhysicsBody.Transform = TargetTransform.WithRotation( TargetTransform.Rotation.RotateAroundAxis( Vector3.Up, (float)Math.Sin( Time.Now ) * 180 ) );
	}

	public bool OnUse( Entity user )
	{
		TargetTransform = TargetTransform.WithPosition(TargetTransform.Position + TargetTransform.Rotation.Forward * 128);

		return false;
	}

	public bool IsUsable( Entity user )
	{
		return true;
	}
}
