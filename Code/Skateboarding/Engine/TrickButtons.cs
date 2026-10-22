using System.Linq;
using Skateboarding.Core;

namespace Skateboarding.Engine;

/// <summary>
/// Tricks on keys. Holding a trick key winds up as the trick stick does pulled down (pushed up with the nollie key held
/// as it goes down), first rolling it a little to the side and back when the trick spins the board (a varial, a hardflip,
/// a 360), and letting go flicks the stick the trick's way: each key pops its own trick through the same reading of the
/// stick as a flick does (SkateTricks), and held longer pops higher, as a held wind-up does. The roll stays under the lean
/// that winds up a shove-it, so the skater winds up as for an ollie. A key let go before its roll is through finishes the
/// roll before it flicks.
/// </summary>
internal sealed class TrickButtons
{
	/// <summary>The trick keys: each control's id (SkateBindings) and its trick, in the docs' order.</summary>
	internal static readonly (string Id, TrickKind Trick, string Name)[] Keys =
	{
		("trick-kickflip", TrickKind.Kickflip, "Kickflip"),
		("trick-shuvit", TrickKind.Shuvit, "Shove-it"),
		("trick-heelflip", TrickKind.Heelflip, "Heelflip"),
		("trick-varial-kickflip", TrickKind.VarialKickflip, "Varial flip"),
		("trick-360-flip", TrickKind.Treflip, "360 flip"),
		("trick-hardflip", TrickKind.Hardflip, "Hardflip"),
		("trick-varial-heelflip", TrickKind.VarialHeelflip, "Varial heelflip"),
		("trick-inward-heelflip", TrickKind.InwardHeelflip, "Inward heelflip"),
		("trick-laser-flip", TrickKind.Laserflip, "Laser flip"),
		("trick-fs-shuvit", TrickKind.FrontsideShuvit, "FS shove-it"),
		("trick-360-shuvit", TrickKind.Shuvit360, "360 shove-it"),
	};

	/// <summary>The tricks on keys, in the same order.</summary>
	internal static readonly TrickKind[] Tricks = Keys.Select( key => key.Trick ).ToArray();

	/// <summary>The tricks' names as the keys show them, in the same order.</summary>
	internal static readonly string[] Names = Keys.Select( key => key.Name ).ToArray();

	// Seconds each point of a wind-up is held (long enough for a physics step to read it at any frame rate) and the flick
	// is held; how far the roll goes to the side (under the 0.5 that leans a wind-up into a shove-it's).
	private const float Hold = 0.04f;
	private const float FlickFor = 0.07f;
	private const float Roll = 0.45f;

	// The points a spinning trick's wind-up rolls through after its first: once to the side and back and to the side
	// again for a half spin, three times for a 360 (more than one stick width of travel is a varial, more than 2.2 a 360:
	// SkateTricks), toward the heels for a backside spin.
	private static readonly float[] Half = { -Roll, 0f, -Roll };
	private static readonly float[] Full = { -Roll, 0f, -Roll, 0f, -Roll, 0f, -Roll };
	private static readonly float[] HalfToes = Half.Select( point => -point ).ToArray();
	private static readonly float[] FullToes = Full.Select( point => -point ).ToArray();

	private TrickKind _trick;
	private bool _nose;
	private float _time;
	private float _flicked = -1f;

	/// <summary>True while a trick key's wind-up or flick plays.</summary>
	internal bool Playing => _trick != TrickKind.None;

	/// <summary>
	/// The trick stick for the trick key held (<see cref="TrickKind.None"/> for none), or null while no trick plays. A key
	/// only starts a trick when <paramref name="ready"/>, as a nollie when <paramref name="nose"/>; one already playing
	/// plays on. X is toward the toes of a regular rider (a frontside spin's way) and is turned for a goofy one.
	/// </summary>
	internal (float X, float Y)? Step( TrickKind held, bool nose, bool goofy, bool ready, float delta )
	{
		if ( _trick == TrickKind.None )
		{
			if ( held == TrickKind.None || !ready )
				return null;
			_trick = held;
			_nose = nose;
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
		return (goofy ? -x : x, _nose ? -y : y);
	}

	private static float[] Rolls( TrickKind trick ) => trick switch
	{
		TrickKind.VarialKickflip or TrickKind.InwardHeelflip => Half,
		TrickKind.Hardflip or TrickKind.VarialHeelflip => HalfToes,
		TrickKind.Treflip or TrickKind.Shuvit360 => Full,
		TrickKind.Laserflip => FullToes,
		_ => System.Array.Empty<float>()
	};

	// Where the flick goes: up and toward the heels for the kickflips (and their varials, the hardflip and the 360 flip),
	// toward the toes for the heelflips, and straight across for the shove-its (toward the heels backside).
	private static (float X, float Y) Flick( TrickKind trick ) => trick switch
	{
		TrickKind.Shuvit or TrickKind.Shuvit360 => (-1f, 0f),
		TrickKind.FrontsideShuvit => (1f, 0f),
		TrickKind.Heelflip or TrickKind.VarialHeelflip or TrickKind.InwardHeelflip or TrickKind.Laserflip => (0.7f, 0.7f),
		_ => (-0.7f, 0.7f)
	};
}
