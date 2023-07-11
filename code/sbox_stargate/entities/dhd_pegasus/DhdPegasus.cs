using System.Collections.Generic;
using System;
using Sandbox;

[Title( "DHD (Pegasus)" ), Category( "Stargate" ), Icon( "chair" ), Spawnable]
public partial class DhdPegasus : Dhd
{
	public Vector3 SpawnOffset { get; private set; } = new( 0, 0, -5 );
	public Angles SpawnOffsetAng { get; private set; } = new( 15, 0, 0 );

	public DhdPegasus()
	{
		Data = new( 1, 1, "dhd.atlantis.press", "dhd.press_dial" );
	}

	public override void Spawn()
	{
		base.Spawn();

		Transmit = TransmitType.Always;
		SetModel( "models/sbox_stargate/dhd/dhd.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic, true );
		PhysicsBody.BodyType = PhysicsBodyType.Static;

		SetMaterialGroup( 1 );

		CreateButtons();

		foreach (var button in Buttons.Values )
		{
			button.SetMaterialGroup( "peg" );
		}
	}
}
