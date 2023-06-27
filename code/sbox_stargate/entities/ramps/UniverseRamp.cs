using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sandbox;

[Title( "Universe Ramp" ), Category( "Stargate" ), Icon( "chair" ), Spawnable]
public partial class UniverseRamp : Prop, IStargateRamp, IGateSpawner
{
	[Net]
	public Vector3 SpawnOffset { get; private set; } = new( 0, 0, 0 );
	public int AmountOfGates => 1;

	public List<CapsuleLightEntity> Lights = new();

	public Vector3[] StargatePositionOffset => new Vector3[] {
		new Vector3( -108.75f, 0, 135 )
	};

	public Angles[] StargateRotationOffset => new Angles[] {
		Angles.Zero
	};

	public List<Stargate> Gate { get; set; } = new();

	public override void Spawn()
	{
		base.Spawn();
		Transmit = TransmitType.Default;

		SetModel( "models/sbox_stargate/ramps/sgu_ramp/sgu_ramp.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic, true );

		Tags.Add( "solid" );

		PostSpawn();
	}

	public async void PostSpawn()
	{
		await GameTask.NextPhysicsFrame();

		for (var i = 1; i <= 4; i++)
		{
			var t = (Transform)GetAttachment( $"light{i}" );
			var light = new CapsuleLightEntity();
			light.Color = Color.FromBytes( 230, 215, 240 );
			light.CapsuleLength = 48;
			light.LightSize = 0.2f;
			light.Position = t.Position;
			light.Rotation = t.Rotation;
			light.Brightness = 0;
			light.SetParent( this );
			Lights.Add( light );
		}

		for ( var i = 5; i <= 8; i++ )
		{
			var t = (Transform)GetAttachment( $"light{i}" );
			var light = new CapsuleLightEntity();
			light.Color = Color.FromBytes( 230, 215, 240 );
			light.CapsuleLength = 22;
			light.LightSize = 0.2f;
			light.Position = t.Position;
			light.Rotation = t.Rotation;
			light.Brightness = 0;
			light.SetParent( this );
			Lights.Add( light );
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		foreach (var light in Lights)
		{
			light?.Delete();
		}
	}

	[GameEvent.Tick.Server]
	private void LightThink()
	{
		var gate = Gate.FirstOrDefault();
		var shouldGlow = gate.IsValid() ? !gate.Idle : false;

		foreach ( var light in Lights )
		{
			light.Brightness = light.Brightness.LerpTo( shouldGlow ? 0.1f : 0f, Time.Delta * (shouldGlow ? 4 : 16) );
		}
	}

	public void FromJson( JsonElement data )
	{
		Position = Vector3.Parse( data.GetProperty( "Position" ).ToString() );
		Rotation = Rotation.Parse( data.GetProperty( "Rotation" ).ToString() );

		PhysicsBody.BodyType = PhysicsBodyType.Static;
	}

	public object ToJson()
	{
		return new JsonModel()
		{
			EntityName = ClassName,
			Position = Position,
			Rotation = Rotation
		};
	}
}
