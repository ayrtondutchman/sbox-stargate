using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

[Spawnable]
[Library( "ent_stargate_movie", Title = "Stargate (Movie)", Group = "Stargate.Stargate" ), Category( "Stargates" )]
public partial class StargateMovie : StargateMilkyWay
{

	public StargateMovie()
	{
		SoundDict = new()
		{
			{ "gate_open", "stargate.movie.open" },
			{ "gate_close", "stargate.movie.close" },
			{ "chevron_open", "stargate.movie.chevron_open" },
			{ "chevron_close", "stargate.movie.chevron_close" },
			{ "dial_fail", "stargate.milkyway.dial_fail_noclose" },
			{ "dial_fail_noclose", "stargate.milkyway.dial_fail_noclose" },
			{ "dial_begin_9chev", "stargate.universe.dial_begin_9chev" },
			{ "dial_fail_9chev", "stargate.universe.dial_fail_9chev" }
		};

		GateGlyphType = GlyphType.MILKYWAY;
		EarthPointOfOrigin = true;

		MovieDialingType = true;
		ChevronLightup = false;
	}

	// SPAWN

	public override void Spawn()
	{
		base.Spawn();
		SetBodyGroup( 0, 1 );

		Ring.StartSoundName = "stargate.movie.ring_roll";
		Ring.StopSoundName = "";
		Ring.StopSoundOnSpinDown = true;

		Ring.SetBodyGroup( 0, 1 );
	}

	// CHEVRONS

	public override Chevron CreateChevron( int n )
	{
		var chev = base.CreateChevron(n);
		chev.UsesDynamicLight = ChevronLightup;

		chev.ChevronStateSkins = new()
		{
			{ "Off", 2 },
			{ "On", ChevronLightup ? 1 : 2 },
		};

		if ( n == 7 ) chev.SetBodyGroup( 0, 1 );

		return chev;
	}

}
