namespace Sandbox.Tools
{
	[Library( "tool_stargate_platforment_test", Title = "PlatformEntTester", Description = "Test platform entities", Group = "construction" )]
	public partial class TestPlatformEntityTool : BaseTool
	{
		public StargateMilkyWay gate;

		public override void Simulate()
		{
			if ( !Host.IsServer )
				return;

			using ( Prediction.Off() )
			{
				if ( Input.Pressed( InputButton.Reload ) )
				{
					var startPos = Owner.EyePosition;
					var dir = Owner.EyeRotation.Forward;

					var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
						.Ignore( Owner )
						.WithAllTags( "solid" )
						.Run();

					if ( !tr.Hit || !tr.Entity.IsValid() )
						return;

					if ( tr.Entity is StargateMilkyWay ent )
					{
						//if (ent.Ring.IsMoving)
						//{
						//	ent.Ring.SpinDown();
						//}
						//else
						//{
						//	ent.Ring.SpinUp();
						//}

						gate = ent;

						ent.PlaySound( "balloon_pop_cute" );
					}
				}

				else if ( Input.Pressed( InputButton.PrimaryAttack ) )
				{
					var startPos = Owner.EyePosition;
					var dir = Owner.EyeRotation.Forward;

					var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
						.Ignore( Owner )
						.WithAllTags( "solid" )
						.Run();

					if ( !tr.Hit || !tr.Entity.IsValid() )
						return;

					if ( tr.Entity is StargateMilkyWay ent )
					{
						ent.Ring.RingCurSpeed = ent.Ring.RingCurSpeed + 5;
						ent.Ring.SetSpeed( ent.Ring.RingCurSpeed );
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

					if ( tr.Entity is StargateMilkyWay ent )
					{
						ent.Ring.RingCurSpeed = ent.Ring.RingCurSpeed - 5;
						ent.Ring.SetSpeed( ent.Ring.RingCurSpeed );
						ent.PlaySound( "balloon_pop_cute" );
					}
				}

			}

			if ( gate.IsValid() ) // simulate doesnt change the speed either, fucked up
			{
				gate.Ring.SetSpeed( gate.Ring.RingCurSpeed );
				Log.Info( $"Setting speed of gate to {gate.Ring.Speed}" );
			}
		}
	}
}
