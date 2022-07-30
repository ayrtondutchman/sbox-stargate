using System;
using System.Threading.Tasks;
using Sandbox;

public partial class RingRing : KeyframeEntity {

	public Rings RingParent;

	public bool isUpsideDown = false;

	private bool reachedPos = false;
	public bool Ready {
		get {
			return reachedPos;
		}
	}
	public Vector3 desiredPos;

	public bool Retract = false;

	public override void Spawn() {
		base.Spawn();
		//Tags.Add( "no_rings_teleport" );

		EnableHitboxes = false;
		PhysicsEnabled = false;
		RenderColor = RenderColor.WithAlpha(0);

		Transmit = TransmitType.Always;
		SetModel( "models/sbox_stargate/rings_ancient/ring_ancient.vmdl" );
	}

	public override void MoveFinished() {
		reachedPos = true;

		if (Retract) {
			RingParent.OnRingReturn();
			Delete();
		}
	}

	public override void MoveBlocked( Entity ent ) {
		var dmg = new DamageInfo();
		dmg.Attacker = RingParent;
		dmg.Damage = 200;
		ent.TakeDamage( dmg );
	}

	public void MoveUp() {
		RenderColor = RenderColor.WithAlpha(1);
		Retract = false;
		Move();
	}

	public async void Move() {
		var targetPos = Retract ? RingParent.Position : RingParent.Transform.PointToWorld( desiredPos );

		//Log.Info( $"BasePos = {RingParent.Position}, TargetPos = {targetPos}" );

		var newTransform = new Transform( targetPos );

		var moveDone = await KeyframeTo( newTransform, 0.2f);

		if ( moveDone )
		{
			MoveFinished();
		}

	}

	public void Refract() {
		Retract = true;
		Move();
	}

}
