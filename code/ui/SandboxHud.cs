using Sandbox;
using Sandbox.UI;

[Library]
public partial class SandboxHud : HudEntity<RootPanel>
{
	public SandboxHud()
	{
		if ( !Game.IsClient )
			return;

		PopulateHud();
	}

	private void PopulateHud()
	{
		RootPanel.StyleSheet.Load( "/ui/SandboxHud.scss" );

		RootPanel.AddChild<ChatBox>();
		RootPanel.AddChild<VoiceList>();
		RootPanel.AddChild<VoiceSpeaker>();
		RootPanel.AddChild<KillFeed>();
		RootPanel.AddChild<Scoreboard<ScoreboardEntry>>();
		RootPanel.AddChild<Health>();
		RootPanel.AddChild<InventoryBar>();
		RootPanel.AddChild<CurrentTool>();
		RootPanel.AddChild<SpawnMenu>();
		RootPanel.AddChild<Crosshair>();
		RootPanel.AddChild<WormholeCinematic>();
	}

	[Event.Hotload]
	private void OnReloaded()
	{
		if ( !Game.IsClient )
			return;

		RootPanel.DeleteChildren();
		PopulateHud();
	}
}
