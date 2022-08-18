using System.Collections.Generic;
using System;
using Sandbox;

[Title( "DHD (Milky Way)" ), Category( "Stargate" ), Icon( "chair" ), Spawnable]
public partial class DhdMilkyWay : Dhd
{
	public Vector3 SpawnOffset { get; private set; } = new( 0, 0, -5 );
	public Angles SpawnOffsetAng { get; private set; } = new( 15, 0, 0 );

	public override void Spawn()
	{
		base.Spawn();

		Transmit = TransmitType.Always;
		SetModel( "models/sbox_stargate/dhd/dhd.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic, true );
		PhysicsBody.BodyType = PhysicsBodyType.Static;

		CreateButtons();
	}
}
