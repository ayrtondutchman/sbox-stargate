using System.Collections.Generic;
using System;
using Sandbox;

[Spawnable]
[Library( "ent_dhd_milkyway", Title = "DHD (Milky Way)", Group = "Stargate.Stargate" )]
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

		CreateButtonTriggers();
		CreateButtons();
	}
}
