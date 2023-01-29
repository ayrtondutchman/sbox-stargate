using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

public class DialingComputerWorldPanel : WorldPanel
{
	private DialingComputer Computer;

	public DialingComputerWorldPanel( DialingComputer computer, ComputerProgramDialing program )
	{
		Computer = computer;

		float size = 1024;
		PanelBounds = new Rect( -size / 2, -size / 2, size, size );

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
	}
	
}
