namespace Skateboarding.Engine.Docs;

/// <summary>Key names as the docs write them on a key chip ("space" is Space, "mouse2" Right mouse), and back.</summary>
internal static class DocsKeyNames
{
	/// <summary>A key as a person would call it.</summary>
	internal static string Pretty( string key )
	{
		if ( string.IsNullOrWhiteSpace( key ) )
			return "Unbound";
		return key.ToLowerInvariant() switch
		{
			"mouse1" or "mouseleft" => "Left mouse",
			"mouse2" or "mouseright" => "Right mouse",
			"mouse3" or "mousemiddle" => "Middle mouse",
			"mouse4" or "mouseback" => "Mouse 4",
			"mouse5" or "mouseforward" => "Mouse 5",
			"space" => "Space",
			"shift" or "lshift" or "left shift" => "Shift",
			"rshift" => "Right shift",
			"ctrl" or "lctrl" or "left ctrl" => "Ctrl",
			"rctrl" => "Right ctrl",
			"alt" or "lalt" or "left alt" => "Alt",
			"ralt" => "Right alt",
			"escape" => "Esc",
			"enter" => "Enter",
			"tab" => "Tab",
			"backspace" => "Backspace",
			"capslock" => "Caps lock",
			"uparrow" or "up" => "Up",
			"downarrow" or "down" => "Down",
			"leftarrow" or "left" => "Left",
			"rightarrow" or "right" => "Right",
			_ => key.Length <= 3 ? key.ToUpperInvariant() : char.ToUpperInvariant( key[ 0 ] ) + key[ 1.. ].ToLowerInvariant()
		};
	}

	/// <summary>A pressed button as the UI names it, turned into the name a binding is stored under.</summary>
	internal static string FromButton( string button )
	{
		if ( string.IsNullOrEmpty( button ) )
			return null;
		return button.ToLowerInvariant() switch
		{
			"mouseleft" => "mouse1",
			"mouseright" => "mouse2",
			"mousemiddle" => "mouse3",
			"mouseback" => "mouse4",
			"mouseforward" => "mouse5",
			var other => other
		};
	}
}
