using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

public class SGCMonitorWorldPanel : WorldPanel
{
	private SGCMonitor Monitor;

	public float RenderSize = 2048;
	public float ActualSize = 2048;

	public SGCMonitorWorldPanel( SGCMonitor monitor, SGCProgram program )
	{
		Monitor = monitor;

		PanelBounds = new Rect( -RenderSize / 2, -RenderSize / 2, RenderSize, RenderSize );

		AddChild( program );

		SceneObject.Flags.BloomLayer = false;
	}

	public override void Tick()
	{
		base.Tick();

		if ( !Monitor.IsValid() )
		{
			Delete();
			return;
		}

		Position = Monitor.Position + Monitor.Rotation.Forward * 30;
		Rotation = Monitor.Rotation;

		var scaleFactor = ActualSize / RenderSize;

		Transform = Transform.WithScale( scaleFactor );
	}
	
}
