using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace Skateboarding.Engine;

/// <summary>
/// Every skate control and its key. On the keyboard and mouse each control has a key of its own, which the player can
/// rebind (the docs' keybinds page) and which is saved in the data folder (skateboard/keybinds.json; a missing or broken
/// file is the defaults). Keys are read as keys (Input.Keyboard), not as the game's input actions, so a binding is the
/// skater's own: while the player rides, the board takes the keyboard from the game (RideKeys). A controller's buttons
/// can only be read through the game's actions, so its map is fixed: each control names what does it there, and the
/// buttons are read as the game binds them.
/// </summary>
internal static class SkateBindings
{
	private const string FilePath = "skateboard/keybinds.json";

	// When a control answers: on the board, carrying it, either (with the board), or at any time (a key bound to one may
	// not be bound to another that answers at the same time).
	private enum Scope { Riding, Carrying, Board, Always }

	// The controls in the order the docs list them: the binding, when it answers, the game action that reads its
	// controller button (none where a stick or a trigger does it, read with the sticks) and that button's glyph.
	private static readonly (SkateBinding Binding, Scope Scope, string PadAction, string PadGlyph)[] Table =
	{
		(new( "push", "Riding", "Push", "w", "A", true ), Scope.Riding, "Jump", "a"),
		(new( "brake", "Riding", "Brake (a foot dragged at speed)", "s", "B", true ), Scope.Riding, "Reload", "b"),
		(new( "steer-left", "Riding", "Steer left", "a", "Left stick", true ), Scope.Riding, null, "leftstickx"),
		(new( "steer-right", "Riding", "Steer right", "d", "Left stick", true ), Scope.Riding, null, "leftstickx"),
		(new( "powerslide", "Riding", "Powerslide (hold) or revert (tap); a foot off in the air", "shift", "LT; L3 in the air", true ),
			Scope.Riding, "attack2", "lefttrigger"),
		(new( "pump", "Riding", "Pump through a transition", "ctrl", "L3, with the left stick held forward", true ),
			Scope.Riding, "Run", "leftjoystickbutton"),
		(new( "manual", "Riding", "Manual (hold); on a rail, onto the tail", "c", "Right stick held part way back", true ),
			Scope.Riding, null, "rightsticky"),
		(new( "nose-manual", "Riding", "Nose manual (hold); on a rail, onto the nose", "b", "Right stick held part way forward", true ),
			Scope.Riding, null, "rightsticky"),
		(new( "hippy-jump", "Riding", "Hippy jump (hold to crouch, let go)", "h", "A + B", true ), Scope.Riding, null, "a"),
		(new( "bail", "Riding", "Bail", "t", "D-pad down", false ), Scope.Riding, "Undo", "dpadsouth"),
		(new( "ollie", "Pops", "Ollie (hold to wind up, let go)", "space", "Right stick back, then forward", true ),
			Scope.Riding, null, "rightsticky"),
		(new( "nollie", "Pops", "Nollie (hold to wind up, let go)", "z", "Right stick forward, then back", true ),
			Scope.Riding, null, "rightsticky"),
		(new( "flip-left", "Pops", "Kickflip side (held as an ollie or nollie pops)", "alt", "Flick the right stick up-left", true ),
			Scope.Riding, null, "rightstickx"),
		(new( "flip-right", "Pops", "Heelflip side (held as an ollie or nollie pops)", "x", "Flick the right stick up-right", true ),
			Scope.Riding, null, "rightstickx"),
		(new( "flick", "Pops", "Trick stick (hold, then move the mouse)", "mouse2", "Right stick", true ), Scope.Riding, null, "rightstickx"),
		(new( "trick-kickflip", "Trick keys", "Kickflip", "1", "Right stick back, then up-left", true ), Scope.Riding, null, "rightstickx"),
		(new( "trick-shuvit", "Trick keys", "Shove-it", "2", "Right stick back, then left", true ), Scope.Riding, null, "rightstickx"),
		(new( "trick-heelflip", "Trick keys", "Heelflip", "3", "Right stick back, then up-right", true ), Scope.Riding, null, "rightstickx"),
		(new( "trick-varial-kickflip", "Trick keys", "Varial kickflip", "4", "Right stick back, rolled left, then up-left", true ),
			Scope.Riding, null, "rightstickx"),
		(new( "trick-360-flip", "Trick keys", "360 flip", "5", "Right stick back, rolled left and back twice, then up-left", true ),
			Scope.Riding, null, "rightstickx"),
		(new( "trick-hardflip", "Trick keys", "Hardflip", "6", "Right stick back, rolled right, then up-left", true ),
			Scope.Riding, null, "rightstickx"),
		(new( "trick-varial-heelflip", "Trick keys", "Varial heelflip", "7", "Right stick back, rolled right, then up-right", true ),
			Scope.Riding, null, "rightstickx"),
		(new( "trick-inward-heelflip", "Trick keys", "Inward heelflip", "8", "Right stick back, rolled left, then up-right", true ),
			Scope.Riding, null, "rightstickx"),
		(new( "trick-laser-flip", "Trick keys", "Laser flip", "9", "Right stick back, rolled right and back twice, then up-right", true ),
			Scope.Riding, null, "rightstickx"),
		(new( "trick-fs-shuvit", "Trick keys", "Frontside shove-it", "0", "Right stick back, then right", true ),
			Scope.Riding, null, "rightstickx"),
		(new( "trick-360-shuvit", "Trick keys", "360 shove-it", "-", "Right stick back, rolled left and back twice, then left", true ),
			Scope.Riding, null, "rightstickx"),
		(new( "grab-front", "In the air", "Grab with the front hand (hold)", "r", "LT in the air; D-pad left on the ground", true ),
			Scope.Riding, "SlotPrev", "lefttrigger"),
		(new( "grab-back", "In the air", "Grab with the back hand (hold)", "f", "RT", true ), Scope.Riding, null, "righttrigger"),
		(new( "catch", "In the air", "Catch, and the dark catch after a flip", "mouse1", "R3", true ),
			Scope.Riding, "View", "rightjoystickbutton"),
		(new( "hand-plant", "In the air", "Hand plant (hold, off a steep lip)", "g", "D-pad up", true ),
			Scope.Riding, "Flashlight", "dpadnorth"),
		(new( "coffin", "In the air", "Coffin (hold)", "y", "D-pad right", true ), Scope.Riding, "SlotNext", "dpadeast"),
		(new( "use", "Board", "Get on and off; hold on foot to pick the board up or call it back", "e", "X", true ),
			Scope.Always, "Use", "x"),
		(new( "throw", "Board", "Throw the board (carrying it)", "mouse1", "RT", false ), Scope.Carrying, "attack1", "righttrigger"),
		(new( "board-editor", "Board", "Board editor: look your board over (carrying it)", "i", "D-pad up", false ),
			Scope.Carrying, "Flashlight", "dpadnorth"),
		(new( "docs", "Board", "Open and close these docs (riding or carrying the board)", "q", "View (Back)", false ),
			Scope.Board, "Score", "switchleftmenu"),
	};

	// Keys no control may take: voice chat, the scoreboard, chat, the menu and the console.
	private static readonly HashSet<string> Reserved = new( StringComparer.OrdinalIgnoreCase )
		{ "v", "tab", "enter", "escape", "`", "backquote" };

	// The keys a rebind listens for, by the names the engine reads them by.
	private static readonly string[] Capturable = "abcdefghijklmnopqrstuvwxyz0123456789".Select( c => c.ToString() )
		.Concat( Enumerable.Range( 1, 12 ).Select( n => $"f{n}" ) )
		.Concat( new[] { "space", "shift", "ctrl", "alt", "mouse1", "mouse2", "mouse3", "mouse4", "mouse5", "backspace",
			"capslock", "uparrow", "downarrow", "leftarrow", "rightarrow", "home", "end", "pgup", "pgdn", "ins", "del", "-" } )
		.ToArray();

	private static Dictionary<string, string> _keys;
	// The simulated keys and buttons as this frame began and as the last one did: a script's change shows from the next
	// frame on, whenever in a frame it is made, as a real key's does.
	private static readonly HashSet<string> _simulatedNow = new( StringComparer.OrdinalIgnoreCase );
	private static readonly HashSet<string> _simulatedBefore = new( StringComparer.OrdinalIgnoreCase );
	private static readonly HashSet<string> _simulatedPadNow = new( StringComparer.OrdinalIgnoreCase );
	private static readonly HashSet<string> _simulatedPadBefore = new( StringComparer.OrdinalIgnoreCase );

	/// <summary>Every control, in the order the docs list them (grouped).</summary>
	internal static IReadOnlyList<SkateBinding> All { get; } = Table.Select( row => row.Binding ).ToArray();

	/// <summary>A binding changed (a rebind or a reset).</summary>
	internal static event Action Changed;

	/// <summary>
	/// Set while the controls must not answer (the docs open, a key being listened for): every control reads as up,
	/// on both devices, but the docs' own.
	/// </summary>
	internal static bool Blocked { get; set; }

	/// <summary>Keys a script holds down as if pressed (a test or a recording), by key name.</summary>
	internal static HashSet<string> Simulated { get; } = new( StringComparer.OrdinalIgnoreCase );

	/// <summary>
	/// Controller buttons a script holds down as if pressed (a test or a recording), by the game action that reads them
	/// ("Jump" for A); with <see cref="SimulatedSticks"/>, a whole controller without one connected.
	/// </summary>
	internal static HashSet<string> SimulatedPad { get; } = new( StringComparer.OrdinalIgnoreCase );

	/// <summary>A controller's sticks and triggers as a script sets them (right-positive X, up-positive Y), or null.</summary>
	internal static Core.Input.ControllerReading? SimulatedSticks { get; set; }

	internal static SkateBinding Get( string id ) => All.FirstOrDefault( binding => binding.Id == id );

	/// <summary>The key the control is bound to now, as the engine names it ("q", "space", "mouse1", "f1").</summary>
	internal static string KeyOf( string id )
	{
		Load();
		return _keys.TryGetValue( id, out var key ) ? key : Get( id )?.DefaultKey;
	}

	/// <summary>True while the control's key is held.</summary>
	internal static bool Down( string id ) => Answers( id ) && KeyDown( KeyOf( id ) );

	/// <summary>True the frame the control's key goes down.</summary>
	internal static bool Pressed( string id ) => Answers( id ) && KeyPressed( KeyOf( id ) );

	/// <summary>True the frame the control's key comes up.</summary>
	internal static bool Released( string id ) => Answers( id ) && KeyReleased( KeyOf( id ) );

	/// <summary>
	/// True while the control's controller button is held. Controls on a stick or a trigger read false here: the sticks
	/// are read as sticks.
	/// </summary>
	internal static bool PadDown( string id ) => Answers( id ) && PadAction( id ) is { } action && PadActionDown( action );

	/// <summary>True the frame the control's controller button goes down.</summary>
	internal static bool PadPressed( string id )
	{
		if ( !Answers( id ) )
			return false;
		// The bail button is the game's undo, which a ride takes from the game the moment it is pressed: RideKeys saw it.
		if ( id == "bail" )
			return Systems.RideKeys.PadBail;
		return PadAction( id ) is { } action && PadActionPressed( action );
	}

	/// <summary>True while the controller holds the button the game action reads (or a script does).</summary>
	internal static bool PadActionDown( string action )
	{
		if ( _simulatedPadNow.Contains( action ) )
			return true;
		using var scope = Input.PlayerScope( Input.PlayerIndex.Controller1 );
		return Input.Down( action, false );
	}

	/// <summary>True the frame the controller's button for the game action goes down (or a script's does).</summary>
	internal static bool PadActionPressed( string action )
	{
		if ( _simulatedPadNow.Contains( action ) && !_simulatedPadBefore.Contains( action ) )
			return true;
		using var scope = Input.PlayerScope( Input.PlayerIndex.Controller1 );
		return Input.Pressed( action );
	}

	/// <summary>True while the control is held on either device.</summary>
	internal static bool Held( string id ) => Down( id ) || PadDown( id );

	/// <summary>True the frame the control is pressed on either device.</summary>
	internal static bool Toggled( string id ) => Pressed( id ) || PadPressed( id );

	/// <summary>Another control already on that key while this one answers too, or null.</summary>
	internal static string ConflictWith( string id, string key )
	{
		var scope = ScopeOf( id );
		foreach ( var row in Table )
		{
			if ( row.Binding.Id != id && Overlaps( scope, row.Scope )
				&& string.Equals( KeyOf( row.Binding.Id ), key, StringComparison.OrdinalIgnoreCase ) )
				return row.Binding.Id;
		}
		return null;
	}

	/// <summary>Whether a key may be bound at all (voice chat, the scoreboard, chat, the menu and the console may not).</summary>
	internal static bool CanBind( string key ) => !string.IsNullOrWhiteSpace( key ) && !Reserved.Contains( key );

	/// <summary>Binds the control to the key: a control already on it takes this one's old key. Saved at once.</summary>
	internal static void Rebind( string id, string key )
	{
		if ( Get( id ) is null || !CanBind( key ) )
			return;
		Load();
		var old = KeyOf( id );
		if ( ConflictWith( id, key ) is { } other )
			_keys[ other ] = old;
		_keys[ id ] = key.ToLowerInvariant();
		Save();
		Changed?.Invoke();
	}

	/// <summary>Every control back on its default key. Saved at once.</summary>
	internal static void ResetAll()
	{
		Load();
		_keys.Clear();
		Save();
		Changed?.Invoke();
	}

	/// <summary>
	/// The key pressed this frame, for a rebind listening for one, or null. The click that started listening is a press
	/// of the left mouse button too, so listen from the frame after it.
	/// </summary>
	internal static string CaptureKey() =>
		Capturable.FirstOrDefault( key => CanBind( key ) && (Input.Keyboard.Pressed( key ) || _simulatedNow.Contains( key ) && !_simulatedBefore.Contains( key )) );

	/// <summary>The control's key as a person reads it: "Space", "Left mouse", "F1", "Q".</summary>
	internal static string KeyLabel( string id ) => Label( KeyOf( id ) );

	/// <summary>The glyph of the control's key (the engine's key glyphs); a blank texture rather than none.</summary>
	internal static Texture KeyGlyph( string id )
	{
		try
		{
			return Input.Keyboard.GetGlyph( KeyOf( id ), InputGlyphSize.Medium, false ) ?? Texture.Transparent;
		}
		catch ( Exception )
		{
			return Texture.Transparent;
		}
	}

	/// <summary>The glyph of what does the control on a controller (a button, a stick or a trigger), or null.</summary>
	internal static Texture PadGlyph( string id )
	{
		var name = Table.FirstOrDefault( row => row.Binding.Id == id ).PadGlyph;
		return name is null ? null : Texture.Load( $"/ui/glyphs/xbox/{name}.svg?w=128&h=128", false );
	}

	/// <summary>
	/// Remembers the simulated keys as this frame leaves them, after everything has read input, so the next frame sees a
	/// key added since as pressed and one taken away as released; RideKeys calls it at the end of every frame.
	/// </summary>
	internal static void EndFrame()
	{
		_simulatedBefore.Clear();
		_simulatedBefore.UnionWith( _simulatedNow );
		_simulatedPadBefore.Clear();
		_simulatedPadBefore.UnionWith( _simulatedPadNow );
	}

	/// <summary>Takes this frame's view of the simulated keys and buttons; RideKeys calls it as each frame begins.</summary>
	internal static void BeginFrame()
	{
		_simulatedNow.Clear();
		_simulatedNow.UnionWith( Simulated );
		_simulatedPadNow.Clear();
		_simulatedPadNow.UnionWith( SimulatedPad );
	}

	/// <summary>The game action that reads the control's controller button, or null.</summary>
	internal static string PadAction( string id ) => Table.FirstOrDefault( row => row.Binding.Id == id ).PadAction;

	// A control answers unless the controls are blocked (the docs' own and the board editor's always do), and one for the
	// board only while the player has it, riding or carrying it.
	private static bool Answers( string id ) =>
		(!Blocked || id is "docs" or "board-editor") && (ScopeOf( id ) != Scope.Board || Systems.RideKeys.WithBoard);

	private static bool KeyDown( string key ) =>
		!string.IsNullOrEmpty( key ) && (Input.Keyboard.Down( key ) || _simulatedNow.Contains( key ));

	private static bool KeyPressed( string key ) =>
		!string.IsNullOrEmpty( key ) && (Input.Keyboard.Pressed( key ) || _simulatedNow.Contains( key ) && !_simulatedBefore.Contains( key ));

	private static bool KeyReleased( string key ) =>
		!string.IsNullOrEmpty( key ) && (Input.Keyboard.Released( key ) || !_simulatedNow.Contains( key ) && _simulatedBefore.Contains( key ));

	private static Scope ScopeOf( string id ) => Table.FirstOrDefault( row => row.Binding.Id == id ).Scope;

	private static bool Overlaps( Scope a, Scope b ) =>
		a == b || a is Scope.Always or Scope.Board || b is Scope.Always or Scope.Board;

	private static string Label( string key ) => key?.ToLowerInvariant() switch
	{
		null or "" => "Unbound",
		"mouse1" => "Left mouse",
		"mouse2" => "Right mouse",
		"mouse3" => "Middle mouse",
		"mouse4" => "Mouse 4",
		"mouse5" => "Mouse 5",
		"uparrow" => "Up",
		"downarrow" => "Down",
		"leftarrow" => "Left",
		"rightarrow" => "Right",
		"pgup" => "Page up",
		"pgdn" => "Page down",
		"capslock" => "Caps lock",
		"-" => "Minus",
		{ Length: 1 } single => single.ToUpperInvariant(),
		{ } name when name.StartsWith( 'f' ) && name.Length <= 3 && char.IsDigit( name[ 1 ] ) => name.ToUpperInvariant(),
		{ } name => char.ToUpperInvariant( name[ 0 ] ) + name[ 1.. ]
	};

	private static void Load()
	{
		if ( _keys is not null )
			return;
		_keys = new Dictionary<string, string>();
		try
		{
			if ( !FileSystem.Data.FileExists( FilePath ) )
				return;
			var saved = Json.Deserialize<Dictionary<string, string>>( FileSystem.Data.ReadAllText( FilePath ) );
			foreach ( var (id, key) in saved ?? new Dictionary<string, string>() )
			{
				if ( Get( id ) is not null && CanBind( key ) )
					_keys[ id ] = key.ToLowerInvariant();
			}
		}
		catch ( Exception )
		{
			_keys.Clear();
		}
	}

	private static void Save()
	{
		try
		{
			FileSystem.Data.WriteAllText( FilePath, Json.Serialize( _keys ) );
		}
		catch ( Exception error )
		{
			Log.Warning( $"Skateboard keybinds not saved: {error.Message}" );
		}
	}
}
