namespace Sandbox.Tools
{
	[Library( "tool_stargate_platforment_test", Title = "PlatformEntTester", Description = "Test platform entities\n\nRELOAD - Toggle Moving\nMOUSE1 - Increase Speed\nMOUSE2 - Decrease Speed", Group = "construction" )]
	public partial class TestPlatformEntityTool : BaseTool
	{

		public override void Simulate()
		{
			if ( !Host.IsServer )
				return;

			using ( Prediction.Off() )
			{
				if ( Input.Pressed( InputButton.PrimaryAttack ) )
				{
					var startPos = Owner.EyePosition;
					var dir = Owner.EyeRotation.Forward;

					var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
						.Ignore( Owner )
						.WithAllTags( "solid" )
						.Run();

					if ( !tr.Hit || !tr.Entity.IsValid() )
						return;

					if ( tr.Entity is TestPlatformEntity ent )
					{
						ent.SetSpeed(ent.Speed + 20);
						if ( ent.Speed > 180 ) ent.SetSpeed( 180 );
						ent.PlaySound( "balloon_pop_cute" );
					}
				}

				else if ( Input.Pressed( InputButton.SecondaryAttack ) )
				{
					var startPos = Owner.EyePosition;
					var dir = Owner.EyeRotation.Forward;

					var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
						.Ignore( Owner )
						.WithAllTags( "solid" )
						.Run();

					if ( !tr.Hit || !tr.Entity.IsValid() )
						return;

					if ( tr.Entity is TestPlatformEntity ent )
					{
						ent.SetSpeed( ent.Speed - 20 );
						if ( ent.Speed < 0 ) ent.SetSpeed( 0 );
						ent.PlaySound( "balloon_pop_cute" );
					}
				}

				else if ( Input.Pressed( InputButton.Reload ) )
				{
					var startPos = Owner.EyePosition;
					var dir = Owner.EyeRotation.Forward;

					var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
						.Ignore( Owner )
						.WithAllTags( "solid" )
						.Run();

					if ( !tr.Hit || !tr.Entity.IsValid() )
						return;

					if ( tr.Entity is TestPlatformEntity ent )
					{
						ent.ToggleMoving();
						ent.PlaySound( "balloon_pop_cute" );
					}
				}

			}
		}
	}
}
