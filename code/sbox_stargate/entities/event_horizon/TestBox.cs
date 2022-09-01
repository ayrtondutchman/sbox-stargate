using System;
using Sandbox;

[Spawnable]
public partial class TestBox : Prop
{
	public override void Spawn()
	{
		base.Spawn();
		SetModel( "models/citizen_props/crate01.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Static );
	}

	[Event.Frame]
	public void Frame()
	{
		var plane = new Plane( WorldSpaceBounds.Center, Rotation.Up );
		SceneObject.Attributes.Set( "EnableClipPlane", true );
		SceneObject.Attributes.Set( "ClipPlane0", new Vector4( plane.Normal, plane.Distance ) );
	}
}
