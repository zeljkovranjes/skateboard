namespace Skateboarding.Engine.Docs;

/// <summary>
/// One move in the docs: what it is, how to do it on the keyboard and on a controller, and its GIF. The how lines are
/// written with tokens filled in live from the bindings, so a rebound key shows as it now is: {id} is that binding's
/// keyboard key, {pad:id} its controller button, {ls} and {rs} the sticks (see <see cref="DocsInputLine"/>).
/// </summary>
/// <param name="Gif">The GIF's id (<see cref="DocsGifs"/>), or null for none.</param>
/// <param name="Title">The move's name.</param>
/// <param name="What">What it is, in a sentence or two.</param>
/// <param name="Keyboard">How to do it on keyboard and mouse.</param>
/// <param name="Controller">How to do it on a controller, or null when <paramref name="Keyboard"/> says it for both (no
/// keys in it) or only the keyboard's way was checked.</param>
/// <param name="Note">Anything else worth knowing, or null.</param>
internal sealed record DocsMove( string Gif, string Title, string What, string Keyboard, string Controller, string Note = null );
