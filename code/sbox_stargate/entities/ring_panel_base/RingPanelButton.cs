using Sandbox;

public partial class RingPanelButton : AnimatedEntity, IUse
{
	public RingPanel RingPanel { get; set; } = null;

	[Net]
	public string Action { get; set; } = "";

	[Net]
	public bool On { get; set; } = false;

	float GlowScale = 0;

	public override void Spawn()
	{
		base.Spawn();
		Tags.Add( "no_rings_teleport" );
		Transmit = TransmitType.Always;
	}
	public bool OnUse( Entity ent )
	{
		RingPanel.TriggerAction( Action );
		return false;
	}

	public bool IsUsable( Entity ent )
	{
		return true;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if ( Game.IsServer ) RingPanel?.Delete();
	}

	public void ButtonGlowLogic()
	{
		var so = SceneObject;
		if ( !so.IsValid() ) return;

		GlowScale = GlowScale.LerpTo( On ? 1 : 0, Time.Delta * (On ? 20f : 10f) );

		so.Batchable = false;
		so.Attributes.Set( "selfillumscale", GlowScale );
	}

	private void DrawButtonActions() // doing anything with world panels is fucking trash, cant position stuff properly, keep debugoverlay for now
	{
		var pos = Transform.PointToWorld( Model.RenderBounds.Center );
		DebugOverlay.Text( Action, pos, Color.White, 0, 86 );
	}

	[GameEvent.Client.Frame]
	public void Think()
	{
		ButtonGlowLogic();
		DrawButtonActions();
	}

}
