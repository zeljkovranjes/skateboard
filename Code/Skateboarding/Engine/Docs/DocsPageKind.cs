namespace Skateboarding.Engine.Docs;

/// <summary>How a docs page is laid out.</summary>
internal enum DocsPageKind
{
	/// <summary>A lead and its moves, each with its GIF.</summary>
	Moves,

	/// <summary>The controller's whole map, button by button.</summary>
	Controller,

	/// <summary>The keyboard's bindings, each one rebindable.</summary>
	Keybinds,

	/// <summary>How loud the boards are, each kind of sound on its own slider.</summary>
	Sound
}
