using Skateboarding.Core;

namespace Skateboarding.Engine;

/// <summary>
/// Tricks on keys. Holding a trick key winds up as the trick stick does pulled down, first rolling it a little to the side
/// and back when the trick spins the board (a varial or a 360 flip), and letting go flicks the stick the trick's way: each
/// key pops its own trick through the same reading of the stick as a flick does (SkateTricks), and held longer pops higher,
/// as a held wind-up does. The roll stays under the lean that winds up a shove-it, so the skater winds up as for an ollie.
/// A key let go before its roll is through finishes the roll before it flicks.
/// </summary>
internal sealed class TrickButtons
{
	/// <summary>The tricks on keys, in the order of <see cref="SandboxActions.TrickKeys"/>.</summary>
	internal static readonly TrickKind[] Tricks =
		{ TrickKind.Kickflip, TrickKind.Shuvit, TrickKind.Heelflip, TrickKind.VarialKickflip, TrickKind.Treflip };

	/// <summary>The tricks' names as the trick keys show them, in the same order.</summary>
	internal static readonly string[] Names = { "Kickflip", "Shove-it", "Heelflip", "Varial flip", "360 flip" };

	// Seconds each point of a wind-up is held (long enough for a physics step to read it at any frame rate) and the flick
	// is held; how far the roll goes to the side (under the 0.5 that leans a wind-up into a shove-it's).
	private const float Hold = 0.04f;
	private const float FlickFor = 0.07f;
	private const float Roll = 0.45f;

	// The points a spinning trick's wind-up rolls through after its first, toward the heels for a backside spin. More than
	// one stick width of travel is a varial, more than 2.2 a 360 (SkateTricks).
	private static readonly float[] Half = { -Roll, 0f, -Roll };
	private static readonly float[] Full = { -Roll, 0f, -Roll, 0f, -Roll, 0f, -Roll };

	private TrickKind _trick;
	private float _time;
	private float _flicked = -1f;

	/// <summary>
	/// The trick stick for the trick key held (<see cref="TrickKind.None"/> for none), or null while no trick plays. A key
	/// only starts a trick when <paramref name="ready"/>; one already playing plays on. X is toward the toes of a regular
	/// rider (a frontside spin's way) and is turned for a goofy one.
	/// </summary>
	internal (float X, float Y)? Step( TrickKind held, bool goofy, bool ready, float delta )
	{
		if ( _trick == TrickKind.None )
		{
			if ( held == TrickKind.None || !ready )
				return null;
			_trick = held;
			_time = 0f;
			_flicked = -1f;
		}
		var rolls = Rolls( _trick );
		float x;
		float y;
		if ( _flicked < 0f && (held == _trick || _time < Hold * (1 + rolls.Length)) )
		{
			var point = (int)(_time / Hold);
			x = point == 0 || rolls.Length == 0 ? 0f : rolls[ Math.Min( point, rolls.Length ) - 1 ];
			y = -1f;
		}
		else
		{
			_flicked = Math.Max( _flicked, 0f );
			if ( _flicked > FlickFor )
			{
				_trick = TrickKind.None;
				return null;
			}
			(x, y) = Flick( _trick );
			_flicked += delta;
		}
		_time += delta;
		return (goofy ? -x : x, y);
	}

	private static float[] Rolls( TrickKind trick ) => trick switch
	{
		TrickKind.VarialKickflip => Half,
		TrickKind.Treflip => Full,
		_ => System.Array.Empty<float>()
	};

	// Where the flick goes: up and toward the heels for a kickflip (and its varial and 360), toward the toes for a
	// heelflip, and straight across toward the heels for a (backside) shove-it.
	private static (float X, float Y) Flick( TrickKind trick ) => trick switch
	{
		TrickKind.Shuvit => (-1f, 0f),
		TrickKind.Heelflip => (0.7f, 0.7f),
		_ => (-0.7f, 0.7f)
	};
}
