using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using Sandbox.UI.Tests;
using System.Linq;

[Library]
public partial class StargateSpawnList : Panel
{
	VirtualScrollPanel Canvas;

	public StargateSpawnList()
	{
		AddClass( "spawnpage" );
		AddChild( out Canvas, "canvas" );

		Canvas.Layout.AutoColumns = true;
		Canvas.Layout.ItemWidth = 100;
		Canvas.Layout.ItemHeight = 100;
		Canvas.OnCreateCell = ( cell, data ) =>
		{
			if ( data is TypeDescription type )
			{
				var btn = cell.Add.Button( type.Title );
				btn.AddClass( "icon" );
				btn.AddEventListener( "onclick", () => ConsoleSystem.Run( "spawn_entity", type.ClassName ) );
				btn.Style.BackgroundImage = Texture.Load( FileSystem.Mounted, $"/entity/sbox_stargate/{type.ClassName}.png", false );
				Log.Info( $"/entity/sbox_stargate/{type.ClassName}.png" );
			}
		};

		var ents = TypeLibrary.GetDescriptions<Entity>().Where( x => x.HasTag( "spawnable" ) && x.Group != null && x.Group.StartsWith("Stargate") ).OrderBy( x => x.Title ).ToArray();

		foreach ( var entry in ents )
		{
			Canvas.AddItem( entry );
		}
	}
}
