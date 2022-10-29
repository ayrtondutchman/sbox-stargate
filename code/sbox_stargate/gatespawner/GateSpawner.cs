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
		var fileName = Global.MapName;

		if ( !FileSystem.Data.DirectoryExists( "gatespawners/" ) )
			FileSystem.Data.CreateDirectory( "gatespawners/" );

		var model = new GatespawnerModel();
		foreach ( IGateSpawner e in Entity.All.OfType<IGateSpawner>() )
		{
			model.Entities.Add( e.ToJson() );
		}
		FileSystem.Data.WriteAllText( $"gatespawners/{fileName}.json", JsonSerializer.Serialize( model, new JsonSerializerOptions() { WriteIndented = true } ) );
	}

	public static async void LoadGateSpawner()
	{
		if ( !Enabled )
		{
			NotEnabledMessage();
			return;
		}

		UnloadGateSpawner(); // unload it before loading, we dont want to have multiple instances of loaded entities

		await Task.Delay( 1000 );

		var filepath = $"{Global.MapName}.json";

		bool isData = FileSystem.Data.FileExists( $"gatespawners/{filepath}" );
		bool isRoot = !isData && FileSystem.Mounted.FileExists( $"code/sbox_stargate/gatespawner/maps/{filepath}" );

		if ( !isData && !isRoot )
			return;

		// Gatespawners in data folder will always have priority
		filepath = (isRoot ? "code/sbox_stargate/gatespawner/maps/" : "gatespawners/") + filepath;

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
}

public class GatespawnerModel
{
	public string Version { get; set; } = "0.0.1";

	public List<object> Entities { get; set; } = new();
}
