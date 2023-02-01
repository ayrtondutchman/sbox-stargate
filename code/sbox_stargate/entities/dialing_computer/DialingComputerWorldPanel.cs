using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

public class DialingComputerWorldPanel : WorldPanel
{
	private DialingComputer Computer;

	public float RenderSize = 2048;
	public float ActualSize = 2048;

	public DialingComputerWorldPanel( DialingComputer computer, Panel program )
	{
		Computer = computer;

		PanelBounds = new Rect( -RenderSize / 2, -RenderSize / 2, RenderSize, RenderSize );

		AddChild( program );

		SceneObject.Flags.BloomLayer = false;
	}

	public override void Tick()
	{
		base.Tick();

		if ( !Computer.IsValid() )
		{
			Delete();
			return;
		}

		Position = Computer.Position + Computer.Rotation.Forward * 30;
		Rotation = Computer.Rotation;

		var scaleFactor = ActualSize / RenderSize;

		Transform = Transform.WithScale( scaleFactor );
	}
	
}
