using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

[Title( "Gate Bearing" ), Category( "Stargate" ), Icon( "chair" )]
public partial class GateBearing : ModelEntity
{
	[Net]
	public bool On { get; private set; } = false;

	[Net]
	public Stargate Gate { get; set; } = null;

	private float glow = 0;

	public override void Spawn()
	{
		base.Spawn();

		Transmit = TransmitType.Always;
		SetModel( "models/sbox_stargate/universe_bearing/universe_bearing.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic, true );
		PhysicsBody.BodyType = PhysicsBodyType.Static;

		Tags.Add( "solid" );
	}

	public async void TurnOn(float delay = 0)
	{
		if ( delay > 0 )
		{
			await Task.DelaySeconds( delay );
			if ( !this.IsValid() ) return;
		}
		
		On = true;
	}

	public async void TurnOff(float delay = 0)
	{
		if ( delay > 0 )
		{
			await Task.DelaySeconds( delay );
			if ( !this.IsValid() ) return;
		}

		On = false;
	}

	[Event.Frame]
	private void GlowLogic()
	{
		if ( !Gate.IsValid() )
			return;

		glow = MathX.Approach( glow, On ? 1 : 0, (On ? 8f : 4f) * Time.Delta );
		SceneObject.Attributes.Set( "selfillumscale", glow );
	}

}
