using Sandbox;
using Sandbox.Component;
using System.Linq;
using System;

public partial class PhysGun
{
	CapsuleLightEntity? BeamLight;
	Particles Beam;
	Particles EndNoHit;

	Vector3 lastBeamPos;
	ModelEntity lastGrabbedEntity;

	Color GrabColor => Color.FromBytes( 15, 190, 215 ) * 0.2f;
	Color IdleColor => Color.FromBytes( 4, 20, 70 ) * 0.2f;

	[GameEvent.Client.Frame]
	public void OnFrame()
	{
		UpdateEffects();
	}

	protected virtual void KillEffects()
	{
		BeamLight?.Delete();
		Beam?.Destroy( true );
		Beam = null;
		BeamActive = false;
		BeamLight = null;

		EndNoHit?.Destroy( false );
		EndNoHit = null;

		if ( lastGrabbedEntity.IsValid() )
		{
			foreach ( var child in lastGrabbedEntity.Children.OfType<ModelEntity>() )
			{
				if ( child is Player )
					continue;

				if ( child.Components.TryGet<Glow>( out var childglow ) )
				{
					childglow.Enabled = false;
				}
			}

			if ( lastGrabbedEntity.Components.TryGet<Glow>( out var glow ) )
			{
				glow.Enabled = false;
			}

			lastGrabbedEntity = null;
		}
	}

	protected virtual void UpdateEffects()
	{
		var owner = Owner as Player;

		if ( owner == null || !BeamActive || owner.ActiveChild != this )
		{
			KillEffects();
			return;
		}

		var startPos = owner.EyePosition;
		var dir = owner.EyeRotation.Forward;

		var tr = Trace.Ray( startPos, startPos + dir * MaxTargetDistance )
			.UseHitboxes()
			.Ignore( owner, false )
			.WithAllTags( "solid" )
			.Run();

		if ( Beam == null )
		{
			Beam = Particles.Create( "particles/physgun_beam.vpcf", tr.EndPosition );
		}

		if ( BeamLight == null )
		{
			BeamLight = new CapsuleLightEntity();
			BeamLight.Color = GrabbedEntity.IsValid() ? GrabColor : IdleColor;
		}

		Beam.SetEntityAttachment( 0, EffectEntity, "muzzle", true );

		if ( GrabbedEntity.IsValid() && !GrabbedEntity.IsWorld )
		{
			var physGroup = GrabbedEntity.PhysicsGroup;

			if ( physGroup != null && GrabbedBone >= 0 )
			{
				var physBody = physGroup.GetBody( GrabbedBone );
				if ( physBody != null )
				{
					Beam.SetPosition( 1, physBody.Transform.PointToWorld( GrabbedPos ) );
				}
			}
			else
			{
				Beam.SetEntity( 1, GrabbedEntity, GrabbedPos, true );
			}

			lastBeamPos = GrabbedEntity.Position + GrabbedEntity.Rotation * GrabbedPos;

			EndNoHit?.Destroy( false );
			EndNoHit = null;

			if ( GrabbedEntity is ModelEntity modelEnt )
			{
				lastGrabbedEntity = modelEnt;

				var glow = modelEnt.Components.GetOrCreate<Glow>();
				glow.Enabled = true;
				glow.Color = new Color( 0.1f, 1.0f, 1.0f, 1.0f );

				foreach ( var child in lastGrabbedEntity.Children.OfType<ModelEntity>() )
				{
					if ( child is Player )
						continue;

					glow = child.Components.GetOrCreate<Glow>();
					glow.Enabled = true;
					glow.Color = new Color( 0.1f, 1.0f, 1.0f, 1.0f );
				}
			}
		}
		else
		{
			lastBeamPos = tr.EndPosition;// Vector3.Lerp( lastBeamPos, tr.EndPosition, Time.Delta * 10 );
			Beam.SetPosition( 1, lastBeamPos );

			if ( EndNoHit == null )
				EndNoHit = Particles.Create( "particles/physgun_end_nohit.vpcf", lastBeamPos );

			EndNoHit.SetPosition( 0, lastBeamPos );
		}

		{
			var muzzle = IsFirstPersonMode && ViewModelEntity.IsValid ? ViewModelEntity.GetAttachment( "muzzle" ) ?? default : GetAttachment( "muzzle" ) ?? default;
			var pos = muzzle.Position;
			var endPos = lastBeamPos;

			BeamLight.CapsuleLength = (pos - endPos).Length * 0.5f;
			BeamLight.LightSize = 0.5f + (MathF.Sin( Time.Now * 10 ) * 0.2f);
			BeamLight.Position = (pos + endPos) * 0.5f;
			BeamLight.Rotation = Rotation.LookAt( (pos - endPos).Normal );

			BeamLight.Color = Color.Lerp( BeamLight.Color, GrabbedEntity.IsValid() ? GrabColor : IdleColor, Time.Delta * 10 );
		}
	}
}
