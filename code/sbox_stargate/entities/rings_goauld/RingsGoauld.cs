using System.Linq;
using Sandbox;

[Title( "Transportation Rings (Goauld)" ), Category( "Stargate" ), Icon( "chair" ), Spawnable]
public partial class RingsGoauld : Rings {
	public override void Spawn()
	{
		base.Spawn();

		Transmit = TransmitType.Always;
		SetModel( "models/sbox_stargate/rings_ancient/ring_ancient.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic, true );
		PhysicsBody.BodyType = PhysicsBodyType.Static;
	}

	protected override void HideBase() {}
	protected override void ShowBase() {}
}
