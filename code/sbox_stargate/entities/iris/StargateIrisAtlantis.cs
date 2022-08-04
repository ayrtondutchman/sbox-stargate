using System;
using Sandbox;

public partial class StargateIrisAtlantis : StargateIris
{
	private readonly float OpenCloseDleay = 1f;
	protected Sound WormholeLoop;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/sbox_stargate/iris_atlantis/iris_atlantis.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Static, true );
		PhysicsBody.BodyType = PhysicsBodyType.Static;

		Transmit = TransmitType.Always;
		Tags.Add( "solid" );
	}

	public async override void Close() {
		if ( Busy || Closed ) return;

		Busy = true;

		Closed = true;
		EnableAllCollisions = true;
		EnableDrawing = true;
		Sound.FromEntity("stargate.iris.atlantis.close", this);

		await Task.DelaySeconds( OpenCloseDleay );
		if ( !this.IsValid() ) return;

		Busy = false;

		await Task.DelaySeconds( 0.6f );
		if ( !this.IsValid() ) return;

		WormholeLoop = Sound.FromEntity( "stargate.iris.atlantis.loop", this );
	}

	public async override void Open() {
		if ( Busy || !Closed ) return;

		WormholeLoop.Stop();

		Busy = true;

		Closed = false;
		EnableAllCollisions = false;
		EnableDrawing = false;
		Sound.FromEntity( "stargate.iris.atlantis.open", this);

		await Task.DelaySeconds( OpenCloseDleay );
		if ( !this.IsValid() ) return;

		Busy = false;
	}

	public override void PlayHitSound() {
		Sound.FromEntity( "stargate.iris.atlantis.hit", this );
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		WormholeLoop.Stop();
	}

	public override void TakeDamage( DamageInfo info )
	{
		base.TakeDamage( info );

		PlayHitSound();
	}

}
