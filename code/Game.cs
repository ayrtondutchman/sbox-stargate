using Sandbox;
using System.Linq;
using System.Threading.Tasks;

partial class SandboxGame : GameManager
{
	public SandboxGame()
	{
		if ( Game.IsServer )
		{
			// Create the HUD
			_ = new SandboxHud();
		}
	}

	public override void ClientJoined( IClient cl )
	{
		base.ClientJoined( cl );
		var player = new SandboxPlayer( cl );
		player.Respawn();

		cl.Pawn = player;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	[ConCmd.Server( "spawn" )]
	public static async Task Spawn( string modelname )
	{
		var owner = ConsoleSystem.Caller?.Pawn as Player;

		if ( ConsoleSystem.Caller == null )
			return;

		var tr = Trace.Ray( owner.EyePosition, owner.EyePosition + owner.EyeRotation.Forward * 4096 )
			.UseHitboxes()
			.Ignore( owner )
			.Run();

		var modelRotation = Rotation.From( new Angles( 0, owner.EyeRotation.Angles().yaw, 0 ) ) * Rotation.FromAxis( Vector3.Up, 180 );

		//
		// Does this look like a package?
		//
		if ( modelname.Count( x => x == '.' ) == 1 && !modelname.EndsWith( ".vmdl", System.StringComparison.OrdinalIgnoreCase ) && !modelname.EndsWith( ".vmdl_c", System.StringComparison.OrdinalIgnoreCase ) )
		{
			modelname = await SpawnPackageModel( modelname, tr.EndPosition, modelRotation, owner as Entity);
			if ( modelname == null )
				return;
		}

		var model = Model.Load( modelname );
		if ( model == null || model.IsError )
			return;

		var ent = new Prop
		{
			Position = tr.EndPosition + Vector3.Down * model.PhysicsBounds.Mins.z,
			Rotation = modelRotation,
			Model = model
		};

		ent.Tags.Add( "undoable" );

		// Let's make sure physics are ready to go instead of waiting
		ent.SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

		// If there's no physics model, create a simple OBB
		if ( !ent.PhysicsBody.IsValid() )
		{
			ent.SetupPhysicsFromOBB( PhysicsMotionType.Dynamic, ent.CollisionBounds.Mins, ent.CollisionBounds.Maxs );
		}
	}

	static async Task<string> SpawnPackageModel( string packageName, Vector3 pos, Rotation rotation, Entity source )
	{
		var package = await Package.Fetch( packageName, false );
		if ( package == null || package.PackageType != Package.Type.Model || package.Revision == null )
		{
			// spawn error particles
			return null;
		}

		if ( !source.IsValid ) return null; // source entity died or disconnected or something

		var model = package.GetMeta( "PrimaryAsset", "models/dev/error.vmdl" );
		var mins = package.GetMeta( "RenderMins", Vector3.Zero );
		var maxs = package.GetMeta( "RenderMaxs", Vector3.Zero );

		// downloads if not downloads, mounts if not mounted
		await package.MountAsync();

		return model;
	}

	[ConCmd.Server( "spawn_entity" )]
	public static void SpawnEntity( string entName )
	{
		var owner = ConsoleSystem.Caller.Pawn as Player;

		if ( owner == null )
			return;

		var entityDesc = TypeLibrary.GetType( entName );
		var entityType = entityDesc.GetType();
		if ( entityType == null )

			if ( !TypeLibrary.HasAttribute<SpawnableAttribute>( entityType ) )
				return;

		var tr = Trace.Ray( owner.EyePosition, owner.EyePosition + owner.EyeRotation.Forward * 4096 )
			.UseHitboxes()
			.Ignore( owner )
			.Size( 2 )
			.Run();

		var ent = entityDesc.Create<Entity>();
		if ( ent is BaseCarriable && owner.Inventory != null )
		{
			if ( owner.Inventory.Add( ent, true ) )
				return;
		}

		ent.Position = tr.EndPosition;
		ent.Rotation = Rotation.From( new Angles( 0, owner.EyeRotation.Angles().yaw + 180, 0 ) );

		ent.Tags.Add( "undoable" ); // cant use Owner, this will need to get reworked at some point, good enough for Singleplayer

		// Stargate Stuffs
		var hasSpawnOffsetProperty = entityDesc.GetProperty( "SpawnOffset" ) != null;
		if ( hasSpawnOffsetProperty ) // spawn offsets for Stargate stuff
		{
			var property_spawnoffset = entityDesc.GetProperty( "SpawnOffset" );
			if ( property_spawnoffset != null ) ent.Position += (Vector3)property_spawnoffset.GetValue( ent );


			var property_spawnoffset_ang = entityDesc.GetProperty( "SpawnOffsetAng" );
			if ( property_spawnoffset_ang != null )
			{
				var ang = (Angles)property_spawnoffset_ang.GetValue( ent );
				var newRot = (ent.Rotation.Angles() + ang).ToRotation();
				ent.Rotation = newRot;
			}

		}

		if ( ent is Stargate gate ) // gate ramps
		{
			if ( tr.Entity is IStargateRamp ramp ) Stargate.PutGateOnRamp( gate, ramp );
		}
		

		//Log.Info( $"ent: {ent}" );
	}

	[ConCmd.Admin( "noclip" )]
	static void DoPlayerNoclip()
	{
		if ( ConsoleSystem.Caller.Pawn is SandboxPlayer basePlayer )
		{
			if ( basePlayer.DevController is NoclipController )
			{
				basePlayer.DevController = null;
			}
			else
			{
				basePlayer.DevController = new NoclipController();
			}
		}
	}

	[ConCmd.Admin( "kill" )]
	static void DoPlayerSuicide()
	{
		if ( ConsoleSystem.Caller.Pawn is SandboxPlayer basePlayer )
		{
			basePlayer.TakeDamage( new DamageInfo { Damage = basePlayer.Health * 99 } );
		}
	}

	[ClientRpc]
	internal static void RespawnEntitiesClient()
	{
		Sandbox.Game.ResetMap( Entity.All.Where( x => !DefaultCleanupFilter( x ) ).ToArray() );
	}

	[ConCmd.Admin( "respawn_entities" )]
	static void RespawnEntities()
	{
		Sandbox.Game.ResetMap( Entity.All.Where( x => !DefaultCleanupFilter( x ) ).ToArray() );
		RespawnEntitiesClient();
	}

	static bool DefaultCleanupFilter( Entity ent )
	{
		// Basic Source engine stuff
		var className = ent.ClassName;
		if ( className == "player" || className == "worldent" || className == "worldspawn" || className == "soundent" || className == "player_manager" )
		{
			return false;
		}

		// When creating entities we only have classNames to work with..
		// The filtered entities below are created through code at runtime, so we don't want to be deleting them
		if ( ent == null || !ent.IsValid ) return true;

		// Gamemode entity
		if ( ent is BaseGameManager ) return false;

		// HUD entities
		if ( ent.GetType().IsBasedOnGenericType( typeof( HudEntity<> ) ) ) return false;

		// Player related stuff, clothing and weapons
		foreach ( var cl in Game.Clients )
		{
			if ( ent.Root == cl.Pawn ) return false;
		}

		// Do not delete view model
		if ( ent is BaseViewModel ) return false;

		return true;
	}

	[ClientRpc]
	public override void OnKilledMessage( long leftid, string left, long rightid, string right, string method )
	{
		KillFeed.Current?.AddEntry( leftid, left, rightid, right, method );
	}

	[ConCmd.Server( "undo" )]
	public static void OnUndoCommand()
	{
		IClient caller = ConsoleSystem.Caller;

		if ( !caller.IsValid() ) return;

		Entity ent = All.LastOrDefault( x => x.Tags.Has( "undoable" ) && (x is not BaseCarriable) );

		if ( ent.IsValid() )
		{
			(caller.Pawn as Entity)?.PlaySound( "balloon_pop_cute" );
			ent.Delete();
		}
	}

}
