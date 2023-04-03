using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

public partial class GateSpawner
{
	[ConVar.Replicated( "gatespawner_enabled" )]
	public static bool Enabled { get; set; }

	public static List<Entity> GateSpawnerEntites = new();

	public static void CreateGateSpawner()
	{
		var fileName = Game.Server.MapIdent;

		if ( !FileSystem.Data.DirectoryExists( "data/gatespawner/" ) )
			FileSystem.Data.CreateDirectory( "data/gatespawner/" );

		var model = new GatespawnerModel();
		foreach ( IGateSpawner e in Entity.All.OfType<IGateSpawner>() )
		{
			model.Entities.Add( e.ToJson() );
		}
		FileSystem.Data.WriteAllText( $"data/gatespawner/{fileName}.json", JsonSerializer.Serialize( model, new JsonSerializerOptions() { WriteIndented = true } ) );
		Log.Info("Created GateSpawner file.");
	}

	//[Event.Hotload]
	public static void GetGatespawnerFileByName(string mapName)
	{
		var fList = new List<string>();
		foreach ( var dir in FileSystem.Mounted.FindDirectory( "", recursive: true ) )
		{
			foreach ( var file in FileSystem.Mounted.FindFile( dir, $"{mapName}.json", true ) )
			{
				var targetName = ( dir + "/" + file );
				if ( !fList.Contains( targetName ) )
					fList.Add( targetName );
			}
		}

		foreach ( var fName in fList )
			Log.Info( fName );
	}

	public static async void LoadGateSpawner()
	{
		if ( !Enabled )
		{
			NotEnabledMessage();
			return;
		}

		UnloadGateSpawner(); // unload it before loading, we dont want to have multiple instances of loaded entities

		await GameTask.Delay( 1000 );

		//GetGatespawnerFileByName( Game.Server.MapIdent );

		var filepath = $"{Game.Server.MapIdent}.json";

		bool isData = FileSystem.Data.FileExists( $"data/gatespawner/{filepath}" );
		bool isRoot = !isData && FileSystem.Mounted.FileExists( $"code/sbox_stargate/gatespawner/maps/{filepath}" );

		if ( !isData && !isRoot )
			return;

		// Gatespawners in data folder will always have priority
		filepath = (isRoot ? "code/sbox_stargate/gatespawner/maps/" : "data/gatespawner/") + filepath;

		var file = isRoot ? FileSystem.Mounted.ReadAllText( filepath ) : FileSystem.Data.ReadAllText( filepath );
		var data = JsonSerializer.Deserialize<GatespawnerModel>( file );

		foreach ( JsonElement o in data.Entities )
		{
			string entityName = o.GetProperty( "EntityName" ).ToString();
			Entity e = TypeLibrary.Create<Entity>( entityName, false );
			if ( e is null || !e.IsValid() )
				continue;

			(e as IGateSpawner).FromJson( o );

			GateSpawnerEntites.Add( e );
		}

		SuccessMessage();
	}

	public static void UnloadGateSpawner()
	{
		if ( !Enabled )
		{
			NotEnabledMessage();
			return;
		}

		var neededUnloading = GateSpawnerEntites.Count > 0;
		foreach ( var ent in GateSpawnerEntites )
		{
			if ( ent.IsValid() ) ent.Delete();
		}
		GateSpawnerEntites.Clear();

		if (neededUnloading)
			SuccessMessage( true );
	}

	private static void NotEnabledMessage()
	{
		Log.Warning("Can't proceed, GateSpawner is not enabled");
	}

	private static void SuccessMessage(bool unloaded = false)
	{
		var action = unloaded ? "unloaded" : "loaded";
		Log.Warning( $"GateSpawner successfully {action}" );
	}

	[ConCmd.Server( "gatespawner" )]
	public static void GateSpawnerCmd( string action )
	{
		switch ( action )
		{
			case "create":
				CreateGateSpawner();
				break;
			case "load":
				LoadGateSpawner();
				break;
			case "unload":
				UnloadGateSpawner();
				break;
		}
	}

	[Event.Entity.PostSpawn]
	private static void PostSpawn()
	{
		LoadGateSpawner();
	}
}

public class GatespawnerModel
{
	public string Version { get; set; } = "0.0.1";

	public List<object> Entities { get; set; } = new();
}
