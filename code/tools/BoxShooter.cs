namespace Sandbox.Tools
{
	[Library( "tool_boxgun", Title = "Box Shooter", Description = "Shoot boxes", Group = "fun" )]
	public class BoxShooter : BaseTool
	{
		TimeSince timeSinceShoot;

		string modelToShoot = "models/citizen_props/crate01.vmdl";

		static bool IsWorldSlow = false;

		public override void Simulate()
		{
			if ( Game.IsServer )
			{
				if ( Input.Pressed( InputButton.Reload ) )
				{
					var tr = Trace.Ray( Owner.EyePosition, Owner.EyePosition + Owner.EyeRotation.Forward * 4000 ).Ignore( Owner ).Run();

					if ( tr.Entity is ModelEntity ent && !string.IsNullOrEmpty( ent.GetModelName() ) )
					{
						modelToShoot = ent.GetModelName();
						Log.Trace( $"Shooting model: {modelToShoot}" );
					}
				}

				if ( Input.Pressed( InputButton.PrimaryAttack ) )
				{
					ShootBox();
				}

				if ( Input.Down( InputButton.SecondaryAttack ) && timeSinceShoot > 0.05f )
				{
					timeSinceShoot = 0;
					ShootBox();
				}

				if ( Input.Pressed( InputButton.Walk ) )
				{
					Game.PhysicsWorld.TimeScale = IsWorldSlow ? 1 : 0.05f;
					IsWorldSlow = !IsWorldSlow;
				}
			}
		}

		void ShootBox()
		{
			var ent = new Prop
			{
				Position = Owner.EyePosition + Owner.EyeRotation.Forward * 50,
				Rotation = Owner.EyeRotation
			};

			ent.SetModel( modelToShoot );
			ent.Velocity = Owner.EyeRotation.Forward * 10000;

			ent.Tags.Add( "undoable" );

			ent.DeleteAsync( 30 );
		}
	}
}
