using System.Collections.Generic;

namespace Skateboarding.Core.Sound;

/// <summary>
/// The recordings a board's sounds are made from (real skateboarding, recorded and released free of rights, cut into
/// single hits and loops; see the addon's README): pops of the tail, landings, a truck striking a metal rail, the board
/// knocking about, and loops of rolling on five grounds, grinding on metal and concrete, a deck sliding and a powerslide.
/// Several of each hit, so the same one is never heard twice in a row.
/// </summary>
internal sealed class SkateSoundBank
{
	/// <summary>Every recording's name (the file under sounds/skate/ it is loaded from, without its extension).</summary>
	internal static readonly string[] Names =
	{
		"pop_1", "pop_2", "pop_3", "pop_4", "pop_5", "pop_6",
		"land_1", "land_2", "land_3", "land_4", "land_5", "land_6", "land_7", "land_8",
		"clank_1", "clank_2", "clank_3", "clank_4", "clank_5", "clank_6", "clank_7",
		"knock_1", "knock_3", "knock_5", "knock_6", "knock_7",
		"roll_park", "roll_street", "roll_wood", "roll_tile", "roll_smooth",
		"grind_metal", "grind_concrete", "slide", "powerslide",
	};

	private readonly List<SoundClip> _pops = new(), _lands = new(), _clanks = new(), _knocks = new();
	private readonly Dictionary<string, SoundClip> _loops = new();
	private readonly Dictionary<List<SoundClip>, int> _last = new();

	/// <summary>Takes in a loaded recording by its name.</summary>
	internal void Add( string name, SoundClip clip )
	{
		if ( clip is null )
			return;
		var group = name.StartsWith( "pop_" ) ? _pops : name.StartsWith( "land_" ) ? _lands
			: name.StartsWith( "clank_" ) ? _clanks : name.StartsWith( "knock_" ) ? _knocks : null;
		if ( group is not null )
			group.Add( clip );
		else
			_loops[ name ] = clip;
	}

	/// <summary>True once the roll the board plays most has loaded.</summary>
	internal bool Ready => _loops.ContainsKey( "roll_park" );

	internal SoundClip Pop( ref SoundNoise noise ) => Pick( _pops, ref noise );
	internal SoundClip Land( ref SoundNoise noise ) => Pick( _lands, ref noise );
	internal SoundClip Clank( ref SoundNoise noise ) => Pick( _clanks, ref noise );
	internal SoundClip Knock( ref SoundNoise noise ) => Pick( _knocks, ref noise );

	/// <summary>The recorded roll of a ground (park concrete until its own has loaded).</summary>
	internal SoundClip Roll( SkateRollRecording recording ) => Loop( recording switch
	{
		SkateRollRecording.Street => "roll_street",
		SkateRollRecording.Wood => "roll_wood",
		SkateRollRecording.Tile => "roll_tile",
		SkateRollRecording.Smooth => "roll_smooth",
		_ => "roll_park"
	} ) ?? Loop( "roll_park" );

	internal SoundClip GrindMetal => Loop( "grind_metal" );
	internal SoundClip GrindConcrete => Loop( "grind_concrete" ) ?? GrindMetal;
	internal SoundClip Slide => Loop( "slide" );
	internal SoundClip Powerslide => Loop( "powerslide" );

	private SoundClip Loop( string name ) => _loops.TryGetValue( name, out var clip ) ? clip : null;

	// One of a group at random, never the one picked last time.
	private SoundClip Pick( List<SoundClip> group, ref SoundNoise noise )
	{
		if ( group.Count == 0 )
			return null;
		var last = _last.TryGetValue( group, out var was ) ? was : -1;
		var pick = (int)(noise.Unit() * group.Count) % group.Count;
		if ( pick == last && group.Count > 1 )
			pick = (pick + 1 + (int)(noise.Unit() * (group.Count - 1)) % (group.Count - 1)) % group.Count;
		_last[ group ] = pick;
		return group[pick];
	}
}
