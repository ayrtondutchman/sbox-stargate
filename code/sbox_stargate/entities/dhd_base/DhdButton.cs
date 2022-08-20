using Sandbox;

public partial class DhdButton : AnimatedEntity, IUse
{
	[Net]
	public Dhd DHD { get; set; } = null;

	[Net]
	public string Action { get; set; } = "";

	[Net]
	public bool On { get; set; } = false;

	[Net]
	public bool Disabled { get; set; } = false;

	public override void Spawn()
	{
		base.Spawn();
		Transmit = TransmitType.Always;
		Health = 100;
	}

	public virtual bool OnUse( Entity user )
	{
		if ( Time.Now < DHD.lastPressTime + DHD.pressDelay ) return false;

		DHD.lastPressTime = Time.Now;
		DHD.TriggerAction( Action, user );

		return false;
	}

	public virtual bool IsUsable( Entity ent )
	{
		return !Disabled;
	}

	public override void TakeDamage( DamageInfo info )
	{
		base.TakeDamage( info );
		//Log.Info( $"{info.Damage} {Health}" );
		if ( Health <= 0 ) Delete();
	}

	[Event.Frame]
	public void ButtonGlowLogic()
	{
		if (DHD.IsValid())
		{
			SetMaterialGroup( On ? DHD.Data.ButtonSkinOn : DHD.Data.ButtonSkinOff );
		}
	}
}
